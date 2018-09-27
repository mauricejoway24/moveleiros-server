using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MovChat.Core.Hub;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MoveleirosChatServer.Channels
{
    [Authorize]
    public class MktPushHub : HubWithPresence
    {
        public MktPushHub(IUserTracker<HubWithPresence> userTracker) : base(userTracker)
        {
        }

        public List<HubConnectionContext> HubConnectionList { get; set; } =
            new List<HubConnectionContext>();

        public override async Task OnConnectedAsync()
        {
            HubConnectionList.Add(base.Context.Connection);

            await CurrentUser.SendAsync("ConfirmToken");

            await base.OnConnectedAsync();
        }
    }
}
