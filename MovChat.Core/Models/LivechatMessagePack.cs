using System;
using MovChat.Core.UI;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace MovChat.Core.Models
{
    public class LivechatMessagePack
    {
        public string Id { get; set; }
        public string FromConnectionId { get; set; }
        public string LivechatUserId { get; set; }
        public string FromName { get; set; }
        public string Message { get; set; }
        public string ChannelId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SerializedContext { get; set; }
        public bool IsBot { get; set; }

        private string _serializedElements;
        public string SerializedElements
        {
            get
            {
                return _serializedElements;
            }

            set
            {
                _serializedElements = value;
                _elements = JsonConvert.DeserializeObject<List<ElementUI>>(value);
            }
        }

        private List<ElementUI> _elements;
        [NotMapped]
        public List<ElementUI> Elements
        {
            get { return _elements; }
            set
            {
                _elements = value;
                _serializedElements = JsonConvert.SerializeObject(value);
            }
        }

        [NotMapped]
        public bool IsPersistent { get; set; } = true;

        public LivechatMessagePack()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
        }
    }
}
