using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovChat.Core.Hub
{
    public class InMemoryUserTracker<THub> : IUserTracker<THub>
    {
        private readonly ConcurrentDictionary<HubConnectionContext, UserDetails> _usersOnline
            = new ConcurrentDictionary<HubConnectionContext, UserDetails>();

        public event Action<UserDetails[]> UsersJoined;
        public event Action<UserDetails[]> UsersLeft;

        public Task<IEnumerable<UserDetails>> UsersOnline(bool includeRemovedUsers = false)
        {
            var users = _usersOnline.Values.AsEnumerable();

            if (!includeRemovedUsers)
                users = users.Where(t => !t.Removed);

            return Task.FromResult(users);
        }

        public Task AddUser(HubConnectionContext connection, UserDetails userDetails)
        {
            _usersOnline.TryAdd(connection, userDetails);
            UsersJoined(new[] { userDetails });

            return Task.CompletedTask;
        }

        public Task RemoveUser(HubConnectionContext connection, bool logicExclusion = false)
        {
            if (logicExclusion)
            {
                if (_usersOnline.ContainsKey(connection))
                    _usersOnline[connection].Removed = true;

                return Task.CompletedTask;
            }

            if (_usersOnline.TryRemove(connection, out var userDetails))
            {
                UsersLeft(new[] { userDetails });
            }

            return Task.CompletedTask;
        }
    }
}