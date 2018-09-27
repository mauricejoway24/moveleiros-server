using System.Collections.Generic;

namespace MoveleirosChatServer.Models
{
    public class EditCustomerProfile
    {
        public string CurrentChannelId { get; set; }
        public Dictionary<string, object> Payload { get; set; } = new Dictionary<string, object>();
    }
}
