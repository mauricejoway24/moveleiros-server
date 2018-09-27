using Microsoft.EntityFrameworkCore;
using MovChat.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovChat.Data.Repositories
{
    public class StoreRepository : IRepositoryBase
    {
        private readonly LivechatContext context;

        public StoreRepository(LivechatContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<Store> GetAICredentials(int storeId)
        {
            var store = context.Store.FindAsync(storeId);
            return store;
        }

        public Task<City> GetCity(int cityId)
        {
            return context.City.FindAsync(cityId);
        }

        public Task<string> GetStoreStyle(int storeId)
        {
            return context.Store
                .Where(t => t.Id == storeId)
                .Select(t => t.StoreStyle)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Customer>> GetAdminsEmailFromStore(int storeId)
        {
            // TODO: Remove hard coded codes
            // 6: Stores
            // 18: ManageLivechat
            var profileAllowToReceiveEmail = new List<int> { 6, 18 };

            var customerIds = await context.StoreMapping
                .Where(t => t.StoreId == storeId && t.EntityName == "Stores")
                .Select(t => t.EntityId).ToListAsync();

            // CustomerRoleMapping is responsable for map a customer role to a customer
            var filterCustomers = await context.CustomerRoleMapping
                .Where(t => profileAllowToReceiveEmail.Contains(t.CustomerRoleId) &&
                    customerIds.Contains(t.CustomerId))
                .Select(t => t.CustomerId)
                .ToListAsync();

            var customers = await context.Customer
                .Where(t => filterCustomers.Contains(t.Id) && !t.Deleted)
                .ToListAsync();

            return customers;
        }
    }
}
