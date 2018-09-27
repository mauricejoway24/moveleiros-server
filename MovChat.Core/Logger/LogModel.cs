using System;

namespace MovChat.Core.Logger
{
    public class LogModel
    {
        public int Id { get; set; }
        public NopLogLevel LogLevelId { get; set; }
        public string ShortMessage { get; set; }
        public string FullMessage { get; set; }
        public DateTime CreatedOnUtc { get; set; } = DateTime.Now;
    }
}
