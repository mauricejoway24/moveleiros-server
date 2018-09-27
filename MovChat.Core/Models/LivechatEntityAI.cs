using System.Collections.Generic;

namespace MovChat.Core.Models
{
    public class LivechatEntityAI
    {
        public string Id { get; set; }
        public string EntityDescription { get; set; }
        public int StoreId { get; set; }
        public List<LivechatEntityValueAI> EntitiyValues { get; set; } = new List<LivechatEntityValueAI>();
    }
}
