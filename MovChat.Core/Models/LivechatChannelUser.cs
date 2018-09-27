using System;

namespace MovChat.Core.Models
{
    public class LivechatChannelUser
    {
        public string Id { get; set; }
        public string LivechatUserId { get; set; }
        public string LivechatChannelId { get; set; }
        public string Payload { get; set; }

        public LivechatChannelUser()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
