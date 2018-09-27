namespace MovChat.Core.Hub
{
    public class TokenDetail
    {
        public string Device { get; set; }
        public string Token { get; set; }

        // When version is filled, this means it's the new mobile version
        public string Version { get; set; }

        public string AuthToken { get; set; }

        public string LivechatUserId { get; set; }
    }
}