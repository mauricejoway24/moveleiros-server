using Microsoft.EntityFrameworkCore;
using MovChat.Core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MovChat.Data.Repositories
{
    public class EntityRepository : IRepositoryBase
    {
        private readonly LivechatContext context;

        public EntityRepository(LivechatContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<LivechatEntityAI> GetEntityAI(int storeId, string entity)
        {
            return context
                .LivechatEntityAI
                .Include(t => t.EntitiyValues)
                .Where(t => t.Id == entity && t.StoreId == storeId)
                .FirstOrDefaultAsync();
        }
    }
}
