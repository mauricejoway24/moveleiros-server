using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MovChat.Core.Models;

namespace MovChat.Data.Repositories
{
    public class LivechatUserTokenRepository : IRepositoryBase
    {
        private readonly LivechatContext context;

        public LivechatUserTokenRepository(LivechatContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task PersistTokenDetail(LivechatUserToken livechatUserToken)
        {
            if (string.IsNullOrEmpty(livechatUserToken.Stores))
                return;

            // Formatting
            var stores = string.Join(",", 
                livechatUserToken.Stores
                    .Split(',')
                    .Select(t => $"|{t}|"));

            livechatUserToken.Stores = stores;

            var dbToken = context.LivechatUserToken
                .Where(t => t.AuthToken == livechatUserToken.AuthToken &&
                    t.Device == livechatUserToken.Device &&
                    t.PushToken == livechatUserToken.PushToken &&
                    t.Stores == stores)
                .FirstOrDefault();

            if (dbToken != null)
                return;

            await context.LivechatUserToken.AddAsync(livechatUserToken);
            await context.SaveChangesAsync();
        }

        public async Task RemoveTokenDetail(LivechatUserToken livechatUserToken)
        {
            var dbToken = context.LivechatUserToken
                .Where(t => t.AuthToken == livechatUserToken.AuthToken &&
                    t.Device == livechatUserToken.Device &&
                    t.PushToken == livechatUserToken.PushToken)
                .FirstOrDefault();

            if (dbToken == null)
                return;

            context.LivechatUserToken.Remove(dbToken);
            await context.SaveChangesAsync();
        }

        public async Task RemoveInvalidToken(string pushToken)
        {
            var dbToken = context.LivechatUserToken
                .Where(t => t.PushToken == pushToken)
                .FirstOrDefault();

            if (dbToken == null)
                return;

            context.LivechatUserToken.Remove(dbToken);
            await context.SaveChangesAsync();
        }

        public Task<List<LivechatUserToken>> GetTokens(string role, int storeId)
        {
            return context.LivechatUserToken
                .Where(t => t.Stores.Contains($"|{storeId.ToString()}|") && t.Role == role)
                .ToListAsync();
        }

        public Task<List<LivechatUserToken>> GetLivechatAgentTokens(string livechatId)
        {
            return context.LivechatUserToken
                .Where(t => t.Device == "android" && t.LivechatUserId == livechatId)
                .ToListAsync();
        }

        public Task<List<LivechatUserToken>> GetLivechatAgentTokenByLiveChatID(string livechatId)
        {
            return context.LivechatUserToken
                .Where(t => t.LivechatUserId == livechatId)
                .ToListAsync();
        }
    }
}
