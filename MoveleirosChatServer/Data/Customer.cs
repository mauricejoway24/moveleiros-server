using System;

namespace MoveleirosChatServer.Data
{
    public class Customer
    {
        public int Id { get; set; }
        public Guid CustomerGuid { get; set; }
        public string CustomerGuidString => CustomerGuid.ToString();
        public string Email { get; set; }
        public string Name { get; set; }
        public CustomerPassword CustomerPassword { get; set; }
    }
}
