using Microsoft.AspNetCore.SignalR;
using MovChat.Core.Hub;
using MovChat.Core.Messaging;
using MovChat.Core.Models;
using MovChat.PluginCore;
using MoveleirosChatServer.Data;
using System;
using System.Threading.Tasks;

namespace MoveleirosChatServer.HubRules
{
    public class CustomerHubRules
    {
        private readonly HubCallerContext context;
        private readonly IHubCallerClients clientsManager;
        private readonly IChatConversationHandler conversationHandler;
        private readonly LivechatRules livechatRules;
        private readonly IGroupManager groupManager;
        private readonly IClientProxy currentUser;

        public CustomerHubRules(
            LivechatRules livechatRules,
            IGroupManager groupManager,
            HubCallerContext context,
            IHubCallerClients clientsManager,
            IChatConversationHandler conversationHandler
        )
        {
            this.livechatRules = livechatRules ?? throw new ArgumentNullException(nameof(livechatRules));
            this.groupManager = groupManager ?? throw new ArgumentNullException(nameof(groupManager));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.clientsManager = clientsManager ?? throw new ArgumentNullException(nameof(clientsManager));
            this.conversationHandler = conversationHandler ?? throw new ArgumentNullException(nameof(conversationHandler));

            currentUser = clientsManager.Client(context.ConnectionId);
        }

        /// <summary>
        /// This method should be call right after a user with Customer profile logs in
        /// </summary>
        /// <param name="userDetails"></param>
        /// <returns></returns>
        public async Task OnCustomerConnectedAsync(UserDetails userDetails)
        {
            if (userDetails == null)
            {
                throw new ArgumentNullException(nameof(userDetails));
            }

            var userStoreId = userDetails.CustomerStoreId;

            if (userStoreId <= 0)
            {
                context.Connection.Abort();
                return;
            }

            // CAUTION
            userDetails.AddStore(userStoreId);

            // Assign to a new session
            var livechatSession = new LivechatUserSession
            {
                ConnectionId = userDetails.ConnectionId,
                LivechatUserId = userDetails.LivechatUserId
            };

            // Include connected user inside Nop Customer table
            await livechatRules.CloneUserToAStore(userDetails.CustomerId, userDetails.CustomerStoreId);

            await livechatRules.CreateNewSession(livechatSession);

            // Check if LivechatId is already registered in some chat with this current store
            // Gets only active stores (IsFinished is false)
            var userCurrentChannel = await livechatRules.GetCustomerChannel(
                userStoreId, 
                livechatSession.LivechatUserId
            );

            // If there is no channel for Customer x Store, it creates a new one and deal with it
            if (userCurrentChannel == null)
            {
                userCurrentChannel = await AssignLivechatUserToChannel(userDetails);

                // Persists current user channel on details so it can be recovery later on online/offline status
                userDetails.Channels.Add(userCurrentChannel);

                // Adds user payload
                await conversationHandler.PersistPayload(
                    livechatUserId: livechatSession.LivechatUserId,
                    channelId: userCurrentChannel.ChannelId,
                    userPayload: userDetails.Payload,
                    overwritePayload: false
                );

                // This is the right time to call a new agent on IChatConversationHandler instance
                await conversationHandler.OnChannelCreated(userCurrentChannel);

                return;
            }

            // If it went this far, is probably because the currentUser's connection went down and its connecting again
            // so basically we should provide its current channel and check if there is some available agent to continue this call
            //
            // Adds current connectionId to correct user group
            // After that it sends the new channel registration to the user
            await NotifyAndAddUserToGroup(
                userDetails.ConnectionId, 
                userCurrentChannel.ChannelId
            );

            // Persists current user channel on details so it can be recovery later on online/offline status
            userDetails.Channels.Add(userCurrentChannel);

            // Adds user payload
            await conversationHandler.PersistPayload(
                livechatUserId: livechatSession.LivechatUserId,
                channelId: userCurrentChannel.ChannelId,
                userPayload: userDetails.Payload,
                overwritePayload: false
            );

            // Verifies if user is alone. If it is try to find a new agent again
            var agentsOnChannel = await conversationHandler.GetAgentsOnChannel(
                userCurrentChannel.ChannelId,
                userDetails.LivechatUserId
            );

            if (agentsOnChannel.Count == 0)
            {
                await conversationHandler.FindNewAgent(userCurrentChannel);
            }
            else
            {
                // TODO: Send an warning telling agent about new users online on this channel
                //foreach (var agent in agentsOnChannel)
                //{
                //    await conversationHandler.SendChannelListToAgent(string.Empty, agent.LivechatUserId);
                //}
            }
        }

        /// <summary>
        /// It creates and assigns a new channel to an user
        /// </summary>
        /// <param name="userDetails">Current user details</param>
        /// <returns>Brand new channel</returns>
        private async Task<LivechatChannel> AssignLivechatUserToChannel(UserDetails userDetails)
        {
            var channelCreated = await livechatRules.CreateChannel(
                userDetails.CustomerStoreId, 
                new LivechatUser
                {
                    Id = userDetails.LivechatUserId,
                    Email = userDetails.Email,
                    Name = userDetails.Name
                }
            );

            await NotifyAndAddUserToGroup(
                userDetails.ConnectionId, 
                channelCreated.ChannelId
            );

            return channelCreated;
        }

        /// <summary>
        /// Add a user connectionId into channel's group
        /// </summary>
        /// <param name="connectionId">Current user connection id</param>
        /// <param name="userCurrentChannelId"></param>
        /// <returns></returns>
        private async Task NotifyAndAddUserToGroup(
            string connectionId, 
            string userCurrentChannelId
        )
        {
            await groupManager.AddAsync(
                connectionId, 
                userCurrentChannelId
            );
            
            // Sends the channel to livechat customer
            await currentUser.SendAsync(
                HubMessages.NEW_CHANNEL_REGISTERED, 
                userCurrentChannelId
            );
        }
    }
}
