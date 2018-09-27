using System.ComponentModel.DataAnnotations;

namespace MoveleirosChatServer.Models
{
    public class ProfilePictureModel
    {
        [Required]
        [MinLength(1)]
        public string Name { get; set; }

        [Range(10, 512)]
        public int Width { get; set; } = 50;

        [Range(10, 512)]
        public int Height { get; set; } = 50;

        public string Color { get; set; }
    }
}
