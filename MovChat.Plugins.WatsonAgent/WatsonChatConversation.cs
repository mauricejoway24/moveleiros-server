using MovChat.PluginCore;
using System.Threading.Tasks;
using MovChat.Core.Messaging;
using MovChat.Core.Models;
using Microsoft.AspNetCore.SignalR;
using MovChat.Core.Hub;
using MovChat.Data.Repositories;
using MovChat.Core.Logger;

namespace MovChat.Plugins.WatsonAgent
{
    public class WatsonChatConversation : DefaultConversationHandler
    {
        public WatsonChatConversation(
            IMessageHubManager messageHubManager,
            IHubCallerClients clientsManager, 
            IUserTracker<HubWithPresence> userTracker, 
            UOW uow) 
            : base(messageHubManager, clientsManager, userTracker, uow)
        {
        }

        public override async Task FindNewAgent(LivechatChannel channel, string clientCallbackMethod = null, int currentNumber = 0)
        {
            var logMessageRepository = uow.GetRepository<LogRepository>();

            await logMessageRepository.RecLog(
                NopLogLevel.Info,
                string.Format(LogMessages.BOT_HAS_ACCEPTED, channel.StoreId)
            );
        }

        public override async Task OnChannelCreated(LivechatChannel channelCreated)
        {
            await SendWelcomeMessage(channelCreated);
            await FindNewAgent(channelCreated);
        }

        public override async Task SendWelcomeMessage(LivechatChannel channel = null)
        {
            await messageHubManager.SendGroupMessage(new LivechatMessagePack
            {
                FromConnectionId = "1",
                FromName = "Moveleiros",
                Message = "Carregando nossa base de conhecimento. Aguarde, por favor.",
                ChannelId = channel.ChannelId,
                IsPersistent = false
            });
        }
    }
}
