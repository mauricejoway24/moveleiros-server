using MovChat.Core.UI;
using System.Collections.Generic;

namespace MovChat.Plugins.WatsonAgent
{
    public class ParsedMessage
    {
        public bool HasUIElements => Elements.Count > 0;
        public List<ElementUI> Elements { get; set; } = new List<ElementUI>();
        public string Message { get; set; }
    }
}
