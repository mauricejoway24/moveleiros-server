using System.ComponentModel.DataAnnotations.Schema;

namespace MovChat.Core.Models
{
    public class City
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string UF { get; set; }
        public string UFDescricao { get; set; }
        public string ShortUrl { get; set; }
    }
}
