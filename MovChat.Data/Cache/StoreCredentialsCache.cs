using MovChat.Core.Models;
using MovChat.Data.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovChat.Data.Cache
{
    public static class StoreCredentialsCache
    {
        public static List<Store> StoreCredentials = new List<Store>();
        public static List<LivechatEntityAI> Entities = new List<LivechatEntityAI>();

        public static async Task<Store> GetStoreAsync(
            UOW uow, 
            int storeId)
        {
            var store = StoreCredentials
                .Where(t => t.Id == storeId)
                .FirstOrDefault();

            if (store != null)
                return store;

            var storeRep = uow.GetRepository<StoreRepository>();
            store = await storeRep.GetAICredentials(storeId);

            if (store == null)
            {
                var log = uow.GetRepository<LogRepository>();
                await log.RecLog(Core.Logger.NopLogLevel.Error, $"Loja não encontrada. Utilizado failover para loja 1. LojaId: {storeId}");

                return await GetStoreAsync(uow, 1);
            }

            StoreCredentials.Add(store);
            return store;
        }

        public static async Task<List<LivechatEntityValueAI>> GetEntityValuesAsync(
            UOW uow, 
            string entity, 
            int storeId)
        {
            var lowerEntity = entity.ToLower();

            var query = Entities
                .Where(t => t.StoreId == storeId && t.Id == lowerEntity);

            var result = query.FirstOrDefault();

            if (result != null)
                return result.EntitiyValues;

            var entityRep = uow.GetRepository<EntityRepository>();
            var entityDb = await entityRep.GetEntityAI(storeId, lowerEntity);

            Entities.Add(entityDb);

            return entityDb.EntitiyValues;
        }
    }
}
