namespace MovChat.Core.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public bool Active { get; set; } = true;
        public bool Deleted { get; set; } = false;
    }
}
