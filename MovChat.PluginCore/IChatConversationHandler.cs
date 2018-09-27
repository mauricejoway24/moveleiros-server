using MovChat.Core.Hub;
using MovChat.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MovChat.PluginCore
{
    public interface IChatConversationHandler
    {
        /// <summary>
        /// It sends a welcome message. The message sent depends on the plugin that was loaded
        /// </summary>
        /// <param name="channel">Optional. You can specify a channel to where the message should be sent</param>
        /// <returns>Task object</returns>
        Task SendWelcomeMessage(LivechatChannel channel = null);

        /// <summary>
        /// It should be called right after a new channel is created. 
        /// When dealing with channels that already exist, use OnChannelMounted
        /// </summary>
        /// <param name="channelCreated">The brand new channel created</param>
        /// <returns>Task object</returns>
        Task OnChannelCreated(LivechatChannel channelCreated);

        /// <summary>
        /// It returns a list of agent on a channel
        /// </summary>
        /// <param name="channelId">Channel to look for</param>
        /// <param name="exceptThisLivechatUserId">If is not null, this will exclude this Id from the result</param>
        /// <returns>A list of users/agent on the channel</returns>
        Task<List<LivechatChannelUser>> GetAgentsOnChannel(string channelId, string exceptThisLivechatUserId);

        /// <summary>
        /// It tries to find an agent to a channel.
        /// It would be executed async so it will no wait for a response
        /// </summary>
        /// <param name="channel">Channel where the agent should be found</param>
        /// <param name="clientCallbackMethod">Override client method</param>
        /// <returns>An async task</returns>
        Task FindNewAgent(LivechatChannel channel, string clientCallbackMethod = null, int currentNumber = 0);

        /// <summary>
        /// Send a list of channels to agent
        /// </summary>
        /// <param name="agentLivechatUserId">Agent id</param>
        /// <param name="connectionId">Agent's connection Id</param>
        /// <returns>List of channels</returns>
        Task<List<LivechatChannel>> SendChannelListToAgent(string connectionId, string agentLivechatUserId);


        /// <summary>
        /// Send a list of channels to all agent's channel
        /// </summary>
        /// <param name="agentLivechatUserId">Agent id</param>
        /// <returns>List of channels</returns>
        Task<List<LivechatChannel>> SendChannelListToAgentChannels(string agentLivechatUserId);

        /// <summary>
        /// Execute a process after an agent connects on hub
        /// </summary>
        /// <param name="agentDetails">Agent's UserDetails</param>
        /// <param name="channelId">Group channel to be sent</param>
        /// <returns>Async obj</returns>
        Task AfterAgentConnected(UserDetails agentDetails, string channelId);

        /// <summary>
        /// It persists the user's dictionary payload
        /// </summary>
        /// <param name="userPayload">User payload</param>
        /// <returns>Async obj</returns>
        Task PersistPayload(string livechatUserId, string channelId, Dictionary<string, object> userPayload, bool overwritePayload = true);

        /// <summary>
        /// It closes the current channelId
        /// </summary>
        /// <param name="channelId">Channel id to be closed</param>
        /// <returns>Async obj</returns>
        Task EndChat(string channelId);
    }
}
