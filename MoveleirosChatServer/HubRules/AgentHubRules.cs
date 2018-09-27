using Microsoft.AspNetCore.SignalR;
using MovChat.Core.Hub;
using MovChat.Core.Models;
using MovChat.PluginCore;
using MoveleirosChatServer.Channels;
using MoveleirosChatServer.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoveleirosChatServer.HubRules
{
    public class AgentHubRules
    {
        private readonly LivechatRules livechatRules;
        private readonly IUserTracker<MktChatHub> userTracker;
        private readonly IGroupManager groupManager;
        private readonly IHubCallerClients clientsManager;
        private readonly IChatConversationHandler conversationHandler;

        public AgentHubRules(
            LivechatRules livechatRules,
            IUserTracker<MktChatHub> userTracker,
            IGroupManager groupManager,
            IHubCallerClients clientsManager,
            IChatConversationHandler conversationHandler
        )
        {
            this.livechatRules = livechatRules ?? throw new System.ArgumentNullException(nameof(livechatRules));
            this.userTracker = userTracker ?? throw new System.ArgumentNullException(nameof(userTracker));
            this.groupManager = groupManager ?? throw new System.ArgumentNullException(nameof(groupManager));
            this.clientsManager = clientsManager ?? throw new System.ArgumentNullException(nameof(clientsManager));
            this.conversationHandler = conversationHandler ?? throw new System.ArgumentNullException(nameof(conversationHandler));
        }

        /// <summary>
        /// This method should be call right after a user with Agent profile logs in
        /// </summary>
        /// <param name="userDetails">Current user details</param>
        /// <returns>A async task</returns>
        public async Task OnAgentConnectedAsync(UserDetails userDetails)
        {
            if (userDetails == null)
            {
                throw new System.ArgumentNullException(nameof(userDetails));
            }

            var currentUserDetails = userDetails;

            // Assign to a new session
            var livechatSession = new LivechatUserSession
            {
                ConnectionId = currentUserDetails.ConnectionId,
                LivechatUserId = currentUserDetails.LivechatUserId
            };

            await livechatRules.CreateNewSession(livechatSession);

            var channels = await conversationHandler.SendChannelListToAgent(currentUserDetails.ConnectionId, livechatSession.LivechatUserId);

            currentUserDetails.Channels = channels;

            // Assign all agent channels to the new connectionId
            await AddAgentChannels(currentUserDetails.ConnectionId, channels);
        }

        /// <summary>
        /// Adds the current agent connection to all its available channels
        /// </summary>
        /// <param name="connectionId">Agent connection id</param>
        /// <param name="channels">All agent's channels</param>
        /// <returns></returns>
        private async Task AddAgentChannels(string connectionId, List<LivechatChannel> channels)
        {
            foreach (var channel in channels)
            {
                await groupManager.AddAsync(connectionId, channel.ChannelId);
            }
        }
    }
}
