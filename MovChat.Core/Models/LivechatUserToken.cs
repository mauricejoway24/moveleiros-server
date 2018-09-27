using System;

namespace MovChat.Core.Models
{
    public class LivechatUserToken
    {
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Device { get; set; }
        public string Version { get; set; }
        public string PushToken { get; set; }
        public string AuthToken { get; set; }
        public string LivechatUserId { get; set; }
        public string Role { get; set; }
        public string Stores { get; set; }

        public LivechatUserToken()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
        }
    }
}
