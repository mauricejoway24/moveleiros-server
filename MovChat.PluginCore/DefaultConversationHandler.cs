using Microsoft.AspNetCore.SignalR;
using MovChat.Core.Hub;
using MovChat.Core.Messaging;
using MovChat.Core.Models;
using MovChat.Data.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovChat.PluginCore
{
    /// <summary>
    /// Definies a default way to handle with conversations
    /// </summary>
    public abstract class DefaultConversationHandler : IChatConversationHandler
    {
        #region Fields

        protected readonly UOW uow;
        protected readonly IMessageHubManager messageHubManager;
        protected readonly IHubCallerClients clientsManager;
        protected readonly IUserTracker<HubWithPresence> userTracker;

        #endregion

        #region ctor

        public DefaultConversationHandler(
            IMessageHubManager messageHubManager,
            IHubCallerClients clientsManager,
            IUserTracker<HubWithPresence> userTracker,
            UOW uow
        )
        {
            this.messageHubManager = messageHubManager ?? throw new System.ArgumentNullException(nameof(messageHubManager));
            this.clientsManager = clientsManager ?? throw new System.ArgumentNullException(nameof(clientsManager));
            this.userTracker = userTracker ?? throw new System.ArgumentNullException(nameof(userTracker));
            this.uow = uow ?? throw new System.ArgumentNullException(nameof(uow));
        }

        #endregion

        #region Abstract methods

        public abstract Task SendWelcomeMessage(LivechatChannel channel = null);
        public abstract Task OnChannelCreated(LivechatChannel channelCreated);
        public abstract Task FindNewAgent(LivechatChannel channel, string clientCallbackMethod = null, int currentNumber = 0);

        #endregion

        #region implemented methods

        /// <summary>
        /// Send a list of channels to agent
        /// </summary>
        /// <param name="agentLivechatUserId">Agent id</param>
        /// <param name="connectionId">Agent's connection Id</param>
        /// <returns>List of channels</returns>
        public virtual async Task<List<LivechatChannel>> SendChannelListToAgent(string connectionId, string agentLivechatUserId)
        {
            var onlineUsers = await userTracker.UsersOnline();

            // Removes current user (agent) from this verification
            onlineUsers = onlineUsers.Where(t => t.LivechatUserId != agentLivechatUserId);

            // Get agent channels
            var channels = await uow
                .GetRepository<ChannelRepository>()
                .GetAgentChannels(agentLivechatUserId);

            // Check list of channels and assign HasOnlineUsers when they have online users
            foreach (var channel in channels)
            {
                channel.HasOnlineUsers = onlineUsers.Any(t =>
                    t.Channels.Any(c => c.ChannelId == channel.ChannelId)
                );
            }

            // Send channels list
            await clientsManager
                .Client(connectionId)
                .SendAsync(HubMessages.ACTIVE_CHANNELS, channels);

            return channels;
        }

        /// <summary>
        /// Send a list of channels to all agent's channel
        /// </summary>
        /// <param name="agentLivechatUserId">Agent id</param>
        /// <returns>List of channels</returns>
        public virtual async Task<List<LivechatChannel>> SendChannelListToAgentChannels(string agentLivechatUserId)
        {
            var onlineUsers = await userTracker.UsersOnline();

            // Removes current user (agent) from this verification
            onlineUsers = onlineUsers.Where(t => t.LivechatUserId != agentLivechatUserId);

            // Get agent channels
            var channels = await uow
                .GetRepository<ChannelRepository>()
                .GetAgentChannels(agentLivechatUserId);

            // Check list of channels and assign HasOnlineUsers when they have online users
            foreach (var channel in channels)
            {
                channel.HasOnlineUsers = onlineUsers.Any(t =>
                    t.Channels.Any(c => c.ChannelId == channel.ChannelId)
                );
            }

            // Get all connections agent is using and send channels
            var agentConnections = onlineUsers.Where(t => t.LivechatUserId == agentLivechatUserId);
            // Send channels list
            foreach (var conn in agentConnections)
            {
                await clientsManager
                    .Client(conn.ConnectionId)
                    .SendAsync(HubMessages.ACTIVE_CHANNELS, channels);
            }

            return channels;
        }

        /// <summary>
        /// Gets agents on channel
        /// </summary>
        /// <param name="channelId">Channel to look for</param>
        /// <param name="exceptThisLivechatUserId">Exclude this user when searching</param>
        /// <returns>List of users/agents</returns>
        public virtual Task<List<LivechatChannelUser>> GetAgentsOnChannel(string channelId, string exceptThisLivechatUserId)
        {
            return uow
                .GetRepository<ChannelRepository>()
                .GetAgentsOnChannel(channelId, exceptThisLivechatUserId);
        }

        /// <summary>
        /// Gets users/connections that are online
        /// </summary>
        /// <returns>List of user details</returns>
        public Task<IEnumerable<UserDetails>> GetUsersOnline(bool includeRemovedUsers = false)
        {
            return userTracker.UsersOnline(includeRemovedUsers);
        }

        /// <summary>
        /// Execute a process after an agent connects on hub
        /// </summary>
        /// <param name="agentDetails">Agent's UserDetails</param>
        /// <param name="channelId">Group channel to be sent</param>
        /// <returns>Async obj</returns>
        public virtual Task AfterAgentConnected(UserDetails agentDetails, string channelId)
        {
            return messageHubManager.SendGroupMessage(new LivechatMessagePack
            {
                FromConnectionId = agentDetails.ConnectionId,
                LivechatUserId = agentDetails.LivechatUserId,
                FromName = agentDetails.Name,
                Message = $"Olá, seja bem-vindo! Meu nome é {agentDetails.Name}, em que posso ajudar?",
                ChannelId = channelId
            });
        }

        /// <summary>
        /// Persists user's payload on database
        /// </summary>
        /// <param name="livechatUserId">User to rec on</param>
        /// <param name="channelId">Channel to rec on</param>
        /// <param name="userPayload">User pauload</param>
        /// <returns>Async obj</returns>
        public Task PersistPayload(
            string livechatUserId, 
            string channelId, 
            Dictionary<string, object> userPayload,
            bool overwritePayload = true)
        {
            return uow
                .GetRepository<UserRepository>()
                .AddPayloadToUser(
                    livechatUserId, 
                    channelId, 
                    userPayload,
                    overwritePayload
                );
        }

        /// <summary>
        /// It closes the current channelId
        /// </summary>
        /// <param name="channelId">Channel id to be closed</param>
        /// <returns>Async obj</returns>
        public Task EndChat(string channelId)
        {
            return uow
                .GetRepository<ChannelRepository>()
                .EndChat(channelId);
        }

        #endregion
    }
}
