using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MovChat.Core.Models
{
    public class LivechatChannel
    {
        public string Id { get; set; }
        public int StoreId { get; set; }
        public string Name { get; set; }
        public bool IsFinished { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<LivechatChannelUser> Users { get; set; }

        // Alias
        public string ChannelId => Id;

        // Pass it just as a complement
        // There is NO information about user state on database
        [NotMapped]
        public bool HasOnlineUsers { get; set; }

        public LivechatChannel()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
