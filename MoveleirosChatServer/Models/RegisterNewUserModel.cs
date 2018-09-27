using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MoveleirosChatServer.Models
{
    public class RegisterNewUserModel
    {
        [Required]
        public string Name { get; set; }

        public string Email { get; set; }

        [Required]
        public string Phone { get; set; }

        public Dictionary<string, object> Payload { get; set; } = 
            new Dictionary<string, object>();
    }
}
