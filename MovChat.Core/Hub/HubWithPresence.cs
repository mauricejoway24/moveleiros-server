using Microsoft.AspNetCore.SignalR;
using MovChat.Core.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovChat.Core.Hub
{
    public class HubWithPresence : Microsoft.AspNetCore.SignalR.Hub
    {
        private IUserTracker<HubWithPresence> _userTracker;

        protected IMessageHubManager MessageHubManager { get; set; }

        protected IClientProxy CurrentUser => Clients.Client(Context.ConnectionId);

        protected async Task<UserDetails> CurrentUserDetails()
        {
            var users = await GetUsersOnline();

            var currentUser = users
                .Where(t => t.ConnectionId == Context.ConnectionId)
                .FirstOrDefault();

            return currentUser;
        }

        public HubWithPresence(IUserTracker<HubWithPresence> userTracker)
        {
            _userTracker = userTracker;
        }

        public Task<IEnumerable<UserDetails>> GetUsersOnline()
        {
            return _userTracker.UsersOnline();
        }

        public virtual Task OnUsersJoined(UserDetails[] user)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnUsersLeft(UserDetails[] user)
        {
            return Task.CompletedTask;
        }
    }
}
