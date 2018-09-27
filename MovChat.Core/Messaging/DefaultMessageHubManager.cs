using System;
using System.Threading.Tasks;
using MovChat.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace MovChat.Core.Messaging
{
    public class DefaultMessageHubManager : IMessageHubManager
    {
        private readonly HubCallerContext hubContext;
        private readonly IHubCallerClients clientsManager;
        private readonly IMessageLogger messageLogger;

        public DefaultMessageHubManager(
            HubCallerContext hubContext,
            IHubCallerClients clientsManager,
            IMessageLogger messageLogger)
        {
            this.hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            this.clientsManager = clientsManager ?? throw new ArgumentNullException(nameof(clientsManager));
            this.messageLogger = messageLogger ?? throw new ArgumentNullException(nameof(messageLogger));
        }

        public IClientProxy CurrentUser => clientsManager.Client(hubContext.ConnectionId);

        public Task InvokeAsync(string method)
        {
            return CurrentUser.SendAsync(method);
        }

        public Task InvokeAsync(string method, object arg1)
        {
            return CurrentUser.SendAsync(method, arg1);
        }

        public Task SendClientMessage(string connectionId, string method, object arg1)
        {
            var client = clientsManager.Client(connectionId);

            if (client == null)
                return Task.CompletedTask;

            return client.SendAsync(method, arg1);
        }

        public async Task SendGroupMessage(LivechatMessagePack messagePack)
        {
            if (messagePack.FromConnectionId == null)
                messagePack.FromConnectionId = hubContext.ConnectionId;

            if (messagePack.IsPersistent)
                await messageLogger.CreateLog(messagePack);

            await clientsManager
                .Group(messagePack.ChannelId)
                .SendAsync(HubMessages.SEND, messagePack);
        }
    }
}
