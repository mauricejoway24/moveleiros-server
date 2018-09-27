using MovChat.Core.Logger;
using System;
using System.Threading.Tasks;

namespace MovChat.Data.Repositories
{
    public class LogRepository : IRepositoryBase
    {
        private readonly LivechatContext context;

        public LogRepository(LivechatContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Write a log on database
        /// </summary>
        /// <param name="logLevel">Defines how critical it is</param>
        /// <param name="shortMessage">Brief of the message</param>
        /// <param name="fullMessage">Full body message</param>
        /// <returns></returns>
        public async Task RecLog(
            NopLogLevel logLevel, 
            string shortMessage, 
            string fullMessage = "")
        {
            var logMessage = new LogModel
            {
                LogLevelId = logLevel,
                ShortMessage = shortMessage,
                FullMessage = fullMessage
            };

            await context.Log.AddAsync(logMessage);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Shortcut to a information log which Auth logs required
        /// </summary>
        /// <param name="shortMessage">Brief of the message</param>
        /// <param name="fullMessage">Full body message</param>
        /// <returns></returns>
        public Task RecAuthLog(
            string shortMessage,
            string fullMessage = "")
        {
            return RecLog(
                NopLogLevel.Info, 
                shortMessage, 
                fullMessage);
        }
    }
}
