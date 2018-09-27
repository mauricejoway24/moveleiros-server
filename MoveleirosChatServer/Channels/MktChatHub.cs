using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using MoveleirosChatServer.Data;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using MoveleirosChatServer.Utils;
using MoveleirosChatServer.HubRules;
using MovChat.Core.Hub;
using MovChat.Core.Models;
using MovChat.Core.Messaging;
using MovChat.Plugins.HumanAgent;
using MovChat.Data.Repositories;
using System;
using MovChat.Core.Logger;
using MovChat.PushNotification;
using MovChat.PluginCore;
using System.Linq;
using System.Collections.Generic;
using MovChat.Data.Cache;
using MovChat.Plugins.WatsonAgent;
using System.Net;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using MoveleirosChatServer.Models;

namespace MoveleirosChatServer.Channels
{
    [Authorize]
    public class MktChatHub : HubWithPresence
    {
        private readonly LivechatRules livechatRules;
        private readonly UOW uow;
        private readonly IConfiguration configuration;
        private readonly IUserTracker<MktChatHub> userTracker;
        private PushNotificationService pushNotificationService;
        private ConcurrentQueue<string> chatQueue = new ConcurrentQueue<string>();
        private readonly LogRepository logRepository;

        public MktChatHub(
            IUserTracker<MktChatHub> userTracker,
            PushNotificationService pushNotificationService,
            LivechatRules livechatRules,
            UOW uow,
            IConfiguration configuration
        ) : base(userTracker)
        {
            this.livechatRules = livechatRules;
            this.uow = uow ?? throw new ArgumentNullException(nameof(uow));
            this.configuration = configuration;
            this.userTracker = userTracker ?? throw new ArgumentNullException(nameof(userTracker));
            this.pushNotificationService = pushNotificationService;

            logRepository = this.uow.GetRepository<LogRepository>();
        }

        /// <summary>
        /// This method is called after a customer or agent connects
        /// </summary>
        /// <returns>Async obj</returns>
        public override async Task OnConnectedAsync()
        {
            var currentUserDetails = await CurrentUserDetails();
            var chatConversationHandler = CreateHumanAgentConversation();

            switch (currentUserDetails.Role)
            {
                case NopRoles.LIVECHAT_AGENT:
                    var agentHubRules = new AgentHubRules(
                        livechatRules: livechatRules,
                        userTracker: userTracker,
                        groupManager: Groups,
                        clientsManager: Clients,
                        conversationHandler: chatConversationHandler
                    );

                    await agentHubRules.OnAgentConnectedAsync(currentUserDetails);
                    break;

                case NopRoles.LIVECHAT_USER:

                    var storeDetails = await StoreCredentialsCache.GetStoreAsync(
                        uow, 
                        currentUserDetails.CustomerStoreId);

                    // If store is configured to use a bot as an agent, why should create it accordingly
                    if (storeDetails != null && storeDetails.ShouldUseWatson)
                        chatConversationHandler = CreateWatsonConversation();

                    var customerHubRules = new CustomerHubRules(
                        livechatRules: livechatRules,
                        context: Context,
                        groupManager: Groups,
                        clientsManager: Clients,
                        conversationHandler: chatConversationHandler
                    );

                    await customerHubRules.OnCustomerConnectedAsync(currentUserDetails);
                    break;

                default:
                    await logRepository.RecLog(
                        NopLogLevel.Error, 
                        LogMessages.ON_CONNECTED_NO_RULES);

                    break;
            }

            await base.OnConnectedAsync();
        }

        #region Hub client messages

        public async Task SendTypingState(LivechatMessagePack message)
        {
            var currentSession = await CurrentUserDetails();
            await Clients
                    .OthersInGroup(message.ChannelId)
                    .SendAsync(HubMessages.TYPING_START, message);
            return;
        }
        /// <summary>
        /// It returns bunch of messages to clients
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task GetLastMessages(string channelId)
        {
            var currentSession = await CurrentUserDetails();

            // Creates a new session for whatever this user is
            var livechatSession = new LivechatUserSession
            {
                ConnectionId = currentSession.ConnectionId,
                LivechatUserId = currentSession.LivechatUserId
            };

            var channel = await livechatRules.GetCurrentUserChannel(livechatSession.LivechatUserId, channelId);

            if (channel == null)
            {
                await CurrentUser.SendAsync(HubMessages.INVALID_CHANNEL);
                return;
            }

            // Gets and sets pack of messages
            var lastMessages = await livechatRules.LoadLastMessages(livechatSession, channel);
            await CurrentUser.SendAsync("SendBulk", lastMessages);
        }

        /// <summary>
        /// It updates payload
        /// </summary>
        /// <param name="extraPayload"></param>
        /// <returns></returns>
        public async Task UpdatePayload(Dictionary<string, object> extraPayload)
        {
            var userDetails = await CurrentUserDetails();
            var userStoreId = userDetails.CustomerStoreId;
            var userCurrentChannel = await livechatRules.GetCustomerChannel(
                userStoreId,
                userDetails.LivechatUserId
            );
            var storeRepository = uow.GetRepository<StoreRepository>();
            var store = await storeRepository.GetAICredentials(userStoreId);
            var city = await storeRepository.GetCity(store.CityId);
            extraPayload.Add("nome", userDetails.Payload["name"]);
            extraPayload.Add("telefone", userDetails.Payload["phone"]);
            extraPayload.Add("loja", userDetails.Payload["currentStore"]);
            extraPayload.Add("store", store);
            extraPayload.Add("city", city);

            var userRepository = uow.GetRepository<UserRepository>();

            //userDetails.Payload[3] = 
            await userRepository.SetNewUserPayloadFromWidget(
                channelId: userCurrentChannel.ChannelId,
                newPayload: extraPayload,
                exceptUserId: userDetails.LivechatUserId);

            await CurrentUser.SendAsync(HubMessages.EDIT_CUSTOMER_PROFILE_SAVED);

            /*
            await conversationHandler.PersistPayload(
                livechatUserId: userDetails.LivechatUserId,
                channelId: userCurrentChannel.ChannelId,
                userPayload: userDetails.Payload,
                overwritePayload: false
            ); */
            return;
        }

        public async Task TryingFindAgent(string channelId, int currentNum)
        {
            var currentSession = await CurrentUserDetails();

            // Creates a new session for whatever this user is
            var livechatSession = new LivechatUserSession
            {
                ConnectionId = currentSession.ConnectionId,
                LivechatUserId = currentSession.LivechatUserId
            };

            var channel = await livechatRules.GetCurrentUserChannel(livechatSession.LivechatUserId, channelId);

            var agent = await livechatRules.GetAgentsOnChannels(channel.Id, currentSession.LivechatUserId);

            if (agent != null)
                return;

            if (channel == null)
            {
                await CurrentUser.SendAsync(HubMessages.INVALID_CHANNEL);
                return;
            }

            var chatConversationHandler = CreateHumanAgentConversation();

            await chatConversationHandler.FindNewAgent(new LivechatChannel
            {
                Id = channelId,
                StoreId = 1
            },
            clientCallbackMethod: HubMessages.LAST_TRYING_FIND_AGENT,
            currentNumber: currentNum
            );
        }

        /// <summary>
        /// It handles group message coming from clients
        /// </summary>
        /// <param name="message">Pack with message to be sent</param>
        /// <returns></returns>
        public async Task SendGroupMessage(LivechatMessagePack message)
        {
            var userDetails = await CurrentUserDetails();
            message.LivechatUserId = userDetails.LivechatUserId;

            var conversationHandler = CreateHubManager();
            await conversationHandler.SendGroupMessage(message);

            // Send a push like message to others in group
            var package = new Dictionary<string, string>
            {
                { "body", message.Message },
                { "title", message.FromName },
                { "icon", "/static/img/launcher-icon-4x.png" },
                { "priority", "high" },
                { "channelId", message.ChannelId },
                { "sound", "default" }
            };

            if (userDetails.Role != "LivechatAgent")
            {
                await Clients
                    .OthersInGroup(message.ChannelId)
                    .SendAsync(HubMessages.NEW_GROUP_MESSAGE, package);

                var agent = await livechatRules.GetAgentsOnChannels(message.ChannelId, message.LivechatUserId);

                if (agent == null)
                    return;

                var userTokenRep = uow.GetRepository<LivechatUserTokenRepository>();
                var agentTokens = await userTokenRep.GetLivechatAgentTokens(agent.LivechatUserId);

                var tokensToSend = new List<TokenDetail>();

                foreach (var tk in agentTokens)
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

                await pushNotificationService.SendPushMessages(tokensToSend,
                    message.ChannelId,
                    userTokenRep,
                    defaultDataAction: HubMessages.NEW_GROUP_MESSAGE,
                    defaultNotificationTitle: message.FromName,
                    defaultNotificationBody: message.Message);
            }
        }

        /// <summary>
        /// This method is called when an agent receives a new token
        /// </summary>
        /// <param name="token">FCM token from agent</param>
        /// <returns>Async method</returns>
        public async Task PushNewToken(TokenDetail token)
        {
            if (token == null || string.IsNullOrEmpty(token.Token))
                return;

            if (string.IsNullOrEmpty(token.Device))
                token.Device = "desktop";

            if (string.IsNullOrEmpty(token.Version))
                token.Version = "pre-alpha";

            var currentUserDetails = await CurrentUserDetails();

            var livechatUserToken = uow.GetRepository<LivechatUserTokenRepository>();

            // TODO: Get AuthToken from query/header instead of param
            await livechatUserToken.PersistTokenDetail(new LivechatUserToken
            {
                AuthToken = currentUserDetails.AuthToken,
                PushToken = token.Token,
                Device = token.Device,
                Version = token.Version,
                LivechatUserId = currentUserDetails.LivechatUserId,
                Role = currentUserDetails.Role,
                Stores = string.Join(",", currentUserDetails.Stores)
            });
        }

        /// <summary>
        /// This methos is called when an agent accepts a new chat
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public async Task JoinNewChannel(string channelId)
        {
            var userDetails = await CurrentUserDetails();

            if (userDetails.Role != NopRoles.LIVECHAT_AGENT)
                return;

            var channelRep = uow.GetRepository<ChannelRepository>();

            // When channel doesn't exist, rec a log and return ok to client
            if (!await channelRep.ChannelExists(channelId))
            {
                var log = uow.GetRepository<LogRepository>();
                await log.RecLog(NopLogLevel.Error, $"Channel doesn't exist {channelId}");

                return;
            }

            var userAlone = await livechatRules.IsCustomerAloneAtRoom(channelId);

            if (!userAlone)
            {
                await CurrentUser.SendAsync(HubMessages.ALREADY_TAKEN);
                return;
            }

            await AddAgentToGroupAndCreateChannel(
                new LivechatUserSession
                {
                    ConnectionId = userDetails.ConnectionId,
                    LivechatUserId = userDetails.LivechatUserId
                },
                new LivechatChannel
                {
                    Id = channelId
                }
            );

            // Persist agent channel
            userDetails.Channels.Add(new LivechatChannel
            {
                Id = channelId
            });

            await logRepository.RecLog(
                NopLogLevel.Info,
                string.Format(LogMessages.AGENT_HAS_ACCEPT_CHAT, userDetails.Name));

            // Let all users connected to this group know that an agent has been connected
            await Clients.Group(channelId).SendAsync(HubMessages.AGENT_HAS_ACCEPTED);

            var chatConversationHandler = CreateHumanAgentConversation();
            // Sends a welcome message
            await chatConversationHandler.AfterAgentConnected(userDetails, channelId);
            // Sends channel list to agent
            await chatConversationHandler.SendChannelListToAgent(userDetails.ConnectionId, userDetails.LivechatUserId);
        }

        /// <summary>
        /// Get customer profile given a channelId. Only for agents
        /// </summary>
        /// <param name="channelId">Current channel Id</param>
        /// <returns>Customer profile</returns>
        public async Task GetCustomerProfile(string channelId)
        {
            var userDetails = await AgentHasChannelPermission(channelId);

            if (userDetails == null)
                return;

            var translatedProfile = await GetUserProfileFromChannel(channelId, userDetails);

            // Sends profile to agent
            await CurrentUser.SendAsync(HubMessages.GET_CUSTOMER_PROFILE, translatedProfile);
        }

        /// <summary>
        /// It should be call when an agent ends a chat
        /// </summary>
        /// <param name="channelId">Channel to be closed</param>
        /// <returns>Async obj</returns>
        public async Task EndChat(string channelId)
        {
            var userDetails = await CurrentUserDetails();

            if (userDetails.Role != NopRoles.LIVECHAT_AGENT)
                return;

            var agentHasPermission = userDetails
                .Channels
                .Any(t => t.Id == channelId);

            if (!agentHasPermission)
            {
                Context.Connection.Abort();
                return;
            }
            await Clients
                .OthersInGroup(channelId)
                .SendAsync(HubMessages.FORWARD_URL, "https://mautic.moveleiros.com.br/form/20");

            var chatConversationHandler = CreateHumanAgentConversation();
            await chatConversationHandler.EndChat(channelId);
            await chatConversationHandler.SendChannelListToAgentChannels(userDetails.LivechatUserId);
            await CurrentUser.SendAsync(HubMessages.CHAT_HAS_ENDED, channelId);
        }

        /// <summary>
        /// It should be call when a user ends a chat
        /// </summary>
        /// <param name="channelId">Channel to be closed</param>
        /// <param name="message">Message to be pushed</param>
        /// <returns>Async obj</returns>
        public async Task CustomerEndChat(string channelId, LivechatMessagePack message)
        {
            var userDetails = await CurrentUserDetails();

            if (userDetails.Role != NopRoles.LIVECHAT_USER)
                return;

            var user = userDetails
                .Channels
                .Any(t => t.Id == channelId);

            if (!user)
            {
                Context.Connection.Abort();
                return;
            }

            message.LivechatUserId = userDetails.LivechatUserId;
            
            // Send a push like message to others in group
            var package = new Dictionary<string, string>
            {
                { "body", message.Message },
                { "title", message.FromName },
                { "icon", "/static/img/launcher-icon-4x.png" },
                { "priority", "high" },
                { "channelId", message.ChannelId },
                { "sound", "default" }
            };

            if (userDetails.Role != "LivechatAgent")
            {
                await Clients
                    .OthersInGroup(message.ChannelId)
                    .SendAsync(HubMessages.END_CHAT_MESSAGE, package);
            }

            var chatConversationHandler = CreateHumanAgentConversation();
            await chatConversationHandler.EndChat(channelId);
            await CurrentUser.SendAsync(HubMessages.FORWARD_URL, "https://mautic.moveleiros.com.br/form/20");
        }

        /// <summary>
        /// This methods sends the current channel customer to a webhook customization
        /// </summary>
        /// <param name="channelId">Current channel</param>
        /// <returns>Async obj</returns>
        public async Task SendCustomerWebhookIntegration(string channelId)
        {
            var userDetails = await CurrentUserDetails();

            if (userDetails.Role != NopRoles.LIVECHAT_AGENT)
                return;

            var agentHasPermission = userDetails
                .Channels
                .Any(t => t.Id == channelId);

            if (!agentHasPermission)
            {
                Context.Connection.Abort();
                return;
            }

            var userRepository = uow.GetRepository<UserRepository>();

            var profile = await userRepository.GetUserPayload(
                channelId: channelId,
                exceptUserId: userDetails.LivechatUserId);

            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";

                var payload = JsonConvert.SerializeObject(profile);

                await client.UploadStringTaskAsync(
                    configuration["CustomerIntegrationWebhook"],
                    payload
                );
            }

            // Sends profile to agent
            await CurrentUser.SendAsync(HubMessages.CUSTOMER_INTEGRATION_SENT);
        }

        /// <summary>
        /// This methods sends the current channel customer to a webhook customization
        /// </summary>
        /// <param name="channelId">Current channel</param>
        /// <returns>Async obj</returns>
        public async Task EditCustomerProfile(string channelId)
        {
            var userDetails = await AgentHasChannelPermission(channelId);

            if (userDetails == null)
                return;

            var translatedProfile = await GetUserProfileFromChannel(channelId, userDetails);

            // Sends profile to agent
            await CurrentUser.SendAsync(HubMessages.EDIT_CUSTOMER_PROFILE, translatedProfile);
        }

        /// <summary>
        /// This method receives a customer to be edit.
        /// </summary>
        /// <param name="customerProfile">Customer info contaning channelId and a payload to be saved</param>
        /// <returns></returns>
        public async Task SaveCustomerProfile(EditCustomerProfile customerProfile)
        {
            var userDetails = await AgentHasChannelPermission(customerProfile.CurrentChannelId);

            if (userDetails == null)
                return;

            var userRepository = uow.GetRepository<UserRepository>();
            var oldPayload = await userRepository.GetUserPayload(customerProfile.CurrentChannelId, userDetails.LivechatUserId);
            oldPayload["email"] = customerProfile.Payload["email"];
            oldPayload["telefone"] = customerProfile.Payload["telefone"];
            // Update customer payload
            await userRepository.SetNewUserPayload(
                channelId: customerProfile.CurrentChannelId,
                newPayload: oldPayload,
                exceptUserId: userDetails.LivechatUserId);

            await CurrentUser.SendAsync(HubMessages.EDIT_CUSTOMER_PROFILE_SAVED);
        }

        /// <summary>
        /// This method is called by a client hub and sends a message to all clients inside
        /// a channel telling them the current client is typing or not
        /// </summary>
        /// <returns>Async task</returns>
        public async Task PushTyping(PushTypingModel pushTyping)
        {
            var currentUser = await CurrentUserDetails();

            var hubMessage = string.Empty;
            if (string.IsNullOrEmpty(pushTyping.Message))
                hubMessage = HubMessages.USER_IS_NOT_TYPING;
            else
                hubMessage = HubMessages.USER_IS_TYPING;

            await Clients.OthersInGroup(pushTyping.ChannelId).SendAsync(hubMessage, new
            {
                channelId = pushTyping.ChannelId,
                livechatUserId = currentUser.LivechatUserId,
                connectionId = currentUser.ConnectionId
            });
        }

        #endregion

        #region Private methods

        private IChatConversationHandler CreateHumanAgentConversation()
        {
            return new HumanAgentConversation(
                CreateHubManager(),
                Clients,
                userTracker,
                uow,
                pushNotificationService
            );
        }

        private IChatConversationHandler CreateWatsonConversation()
        {
            return new WatsonChatConversation(
                messageHubManager: CreateHubManager(),
                clientsManager: Clients,
                userTracker: userTracker,
                uow: uow
            );
        }

        private IMessageHubManager CreateHubManager()
        {
            return new DefaultMessageHubManager(
                hubContext: Context,
                clientsManager: Clients,
                messageLogger: uow.GetRepository<MessageLogRepository>()
            );
        }

        private async Task AddAgentToGroupAndCreateChannel(LivechatUserSession livechatSession, LivechatChannel userCurrentChannel)
        {
            await Groups.AddAsync(livechatSession.ConnectionId, userCurrentChannel.ChannelId);

            await livechatRules.AddUserToChannel(new LivechatChannelUser
            {
                LivechatChannelId = userCurrentChannel.ChannelId,
                LivechatUserId = livechatSession.LivechatUserId
            });

            // Sends the channel to livechat customer
            await CurrentUser.SendAsync("NewChannelRegistry", userCurrentChannel.ChannelId);
        }

        /// <summary>
        /// Check if current user is an agent and has permission to given channelId
        /// </summary>
        /// <param name="channelId">Channel id to check</param>
        /// <returns>Current user details, if null, user doesn't have permission</returns>
        private async Task<UserDetails> AgentHasChannelPermission(string channelId)
        {
            var userDetails = await CurrentUserDetails();

            if (userDetails.Role != NopRoles.LIVECHAT_AGENT)
                return null;

            var agentHasPermission = userDetails
                .Channels
                .Any(t => t.Id == channelId);

            if (!agentHasPermission)
            {
                Context.Connection.Abort();
                return null;
            }

            return userDetails;
        }

        private async Task<Dictionary<string, object>> GetUserProfileFromChannel(string channelId, UserDetails userDetails)
        {
            var userRepository = uow.GetRepository<UserRepository>();

            var profile = await userRepository.GetUserPayload(
                channelId: channelId,
                exceptUserId: userDetails.LivechatUserId);

            var translatedProfile = new Dictionary<string, object>();

            foreach (var p in profile)
            {
                switch (p.Key)
                {
                    case "name":
                        translatedProfile.Add("Nome", p.Value);
                        break;
                    case "email":
                        translatedProfile.Add("Email", p.Value);
                        break;
                    case "phone":
                        translatedProfile.Add("Telefone", p.Value);
                        break;
                    case "currentStore":
                        translatedProfile.Add("Loja", p.Value.ToString() == "7" ? "Buscador" : p.Value);
                        break;
                    default:
                        translatedProfile.Add(p.Key, p.Value);
                        break;
                }
            }

            return translatedProfile;
        }

        #endregion
    }
}
