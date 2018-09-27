using MovChat.Core.Messaging;
using MovChat.Core.Models;
using MovChat.PluginCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MovChat.Core.Hub;
using MovChat.Data.Repositories;
using System.Linq;
using MovChat.Core.Logger;
using MovChat.PushNotification;
using System.Collections.Generic;

namespace MovChat.Plugins.HumanAgent
{
    public class HumanAgentConversation : DefaultConversationHandler
    {
        private readonly PushNotificationService pushNotificationService;

        public HumanAgentConversation(
            IMessageHubManager messageHubManager,
            IHubCallerClients clientsManager,
            IUserTracker<HubWithPresence> userTracker,
            UOW uow,
            PushNotificationService pushNotificationService
        ) : base(
                messageHubManager, 
                clientsManager, 
                userTracker, 
                uow
            )
        {
            this.pushNotificationService = pushNotificationService ?? throw new System.ArgumentNullException(nameof(pushNotificationService));
        }

        public override async Task FindNewAgent(LivechatChannel channel, string clientCallbackMethod = null, int currentNumber = 0)
        {
            var logMessageRepository = uow.GetRepository<LogRepository>();
            var tokenRepository = uow.GetRepository<LivechatUserTokenRepository>();

            // Get all agents from the store requested
            // PS: PushToken can't be empty because is the only way to stay in touch
            var mobileAgents = await tokenRepository.GetTokens("LivechatAgent", channel.StoreId);

            var onlineUsers = await GetUsersOnline();

            IEnumerable<UserDetails> desktopAgents = new List<UserDetails>();

            if (onlineUsers != null)
            {
                desktopAgents = onlineUsers
                    .Where(t => t.Role == "LivechatAgent" &&
                        t.Stores.Contains(channel.StoreId) &&
                        t.Device.ToLower() == "desktop");
            }

            // If there is no agent available, try Moveleiros. If still there is no agent, request a "no agent online" form
            if (mobileAgents.Count == 0 && desktopAgents.Count() == 0)
            {
                // Try calling moveleiros
                // Main store Id = 1 
                mobileAgents = await tokenRepository.GetTokens("LivechatAgent", 1);

                if (onlineUsers != null)
                {
                    desktopAgents = onlineUsers
                        .Where(t => t.Role == "LivechatAgent" &&
                            t.Stores.Contains(1) &&
                            t.Device.ToLower() == "desktop");
                }

                if (mobileAgents.Count == 0 && desktopAgents.Count() == 0)
                {
                    await logMessageRepository.RecLog(
                        NopLogLevel.Info, 
                        string.Format(LogMessages.NO_AGENTS_AVAILABLE, channel.StoreId)
                    );

                    // No agent online :(
                    await messageHubManager.InvokeAsync(HubMessages.NO_AGENT_AVAILABLE);

                    return;
                }
            }

            // Send messages to all available agents
            var tokensToSend = new List<TokenDetail>();

            foreach (var tk in mobileAgents)
            {
                if (tokensToSend.Any(t => t.Token == tk.PushToken))
                    continue;

                tokensToSend.Add(new TokenDetail
                {
                    LivechatUserId = tk.LivechatUserId,
                    Device = tk.Device,
                    Token = tk.PushToken
                });
            }

            foreach (var tk in desktopAgents)
            {
                // Inside desktop version 1.0.1, connectionId is considered as push token
                if (tokensToSend.Any(t => t.Token == tk.ConnectionId))
                    continue;

                tokensToSend.Add(new TokenDetail
                {
                    LivechatUserId = tk.LivechatUserId,
                    Device = tk.Device,
                    Token = tk.ConnectionId,
                    Version = tk.Version
                });
            }

            await pushNotificationService.SendPushMessages(tokensToSend, 
                channel.ChannelId,
                tokenRepository,
                messageHubManager,
                logMessageRepository);

            await logMessageRepository.RecLog(
                NopLogLevel.Info,
                string.Format(LogMessages.PUSH_SENT_TO_STORE, channel.StoreId)
            );

            // Now that everyone knows about the customer, we should alert livechat that it should start counting before
            // quit chat and give up waiting
            // PS: the second parameter is the time client app is gonna wait
            // Default: 1 min
            if (clientCallbackMethod == null)
            {
                await messageHubManager.InvokeAsync(HubMessages.TRYING_FIND_AGENT, new
                {
                    channelId = channel.ChannelId,
                    timeToWait = 1 * 10 * 1000,
                    currentNum = 0
                });
            } else
            {
                await messageHubManager.InvokeAsync(currentNumber > 5 ? clientCallbackMethod : HubMessages.TRYING_FIND_AGENT, new
                {
                    channelId = channel.ChannelId,
                    timeToWait = 1 * 10 * 1000,
                    currentNum = currentNumber
                });
            }
        }

        public override async Task OnChannelCreated(LivechatChannel channelCreated)
        {
            await SendWelcomeMessage(channelCreated);
            await FindNewAgent(channelCreated);
        }

        public override async Task SendWelcomeMessage(LivechatChannel channel)
        {
            if (channel == null)
            {
                throw new System.ArgumentNullException(nameof(channel));
            }

            await messageHubManager.SendGroupMessage(new LivechatMessagePack
            {
                FromConnectionId = "1",
                FromName = "Moveleiros",
                Message = "Seja bem-vindo! Estamos entrando em contato com a loja. Aguarde, por favor ;)",
                ChannelId = channel.ChannelId
            });
        }
    }
}
