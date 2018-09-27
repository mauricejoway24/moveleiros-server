namespace MovChat.Core.Models
{
    public class LivechatUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int? CustomerId { get; set; }
    }
}
