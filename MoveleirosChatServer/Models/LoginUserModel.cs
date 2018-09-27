using System.ComponentModel.DataAnnotations;

namespace MoveleirosChatServer.Models
{
    public class LoginUserModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public string Device { get; set; }
        public string Version { get; set; }
    }
}
