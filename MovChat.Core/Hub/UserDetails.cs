using MovChat.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace MovChat.Core.Hub
{
    public class UserDetails
    {
        public string ConnectionId { get; }
        public string Name { get; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string LivechatUserId { get; set; }
        public List<int> Stores { get; set; } = new List<int>();
        public List<TokenDetail> PushToken { get; set; } = new List<TokenDetail>();
        public List<LivechatChannel> Channels { get; set; } = new List<LivechatChannel>();
        public int CustomerStoreId { get; set; } = 0;
        public Dictionary<string, object> Payload = new Dictionary<string, object>();
        public int? CustomerId { get; set; }
        public string AuthToken { get; set; }
        public string Device { get; set; }
        public string Version { get; set; }

        public bool Removed { get; set; } = false;

        public UserDetails(string connectionId, string name)
        {
            ConnectionId = connectionId;
            Name = name;
        }

        public void AddStore(int storeId)
        {
            if (Stores.Any(t => t == storeId))
                return;

            Stores.Add(storeId);
        }
    }
}