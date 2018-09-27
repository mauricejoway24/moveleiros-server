using System.ComponentModel.DataAnnotations.Schema;

namespace MovChat.Core.Models
{
    public class Store
    {
        public int Id { get; set; }
        public bool? IsWatson { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string WorkspaceId { get; set; }
        public string StoreStyle { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyPhoneNumber { get; set; }
        public int CityId { get; set; }

        [NotMapped]
        public bool ShouldUseWatson => IsWatson.HasValue ? IsWatson.Value : false;
    }
}
