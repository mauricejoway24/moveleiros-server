using MovChat.Core.Messaging;
using MovChat.Core.Models;
using System;
using System.Threading.Tasks;

namespace MovChat.Data.Repositories
{
    public class MessageLogRepository : IMessageLogger, IRepositoryBase
    {
        private readonly LivechatContext context;

        public MessageLogRepository(LivechatContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task CreateLog(LivechatMessagePack pack)
        {
            await context.LivechatMessagePack.AddAsync(pack);
            await context.SaveChangesAsync();
        }
    }
}
