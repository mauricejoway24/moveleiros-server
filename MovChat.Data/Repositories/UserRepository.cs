using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovChat.Data.Repositories
{
    public class UserRepository : IRepositoryBase
    {
        private readonly LivechatContext context;

        public UserRepository(LivechatContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// It adds a simple payload in json format joint with customer
        /// </summary>
        /// <param name="livechatUserId">User livechatId</param>
        /// <param name="payload">Dictionary format payload</param>
        /// <returns>Async method</returns>
        public async Task AddPayloadToUser(
            string livechatUserId, 
            string channelId, 
            Dictionary<string, object> payload,
            bool overwritePayload = true)
        {
            var dbPayload = await context
                .LivechatChannelUser
                .FirstOrDefaultAsync(t => 
                    t.LivechatUserId == livechatUserId &&
                    t.LivechatChannelId == channelId
                );

            if (dbPayload == null)
                return;

            if (!overwritePayload && !string.IsNullOrEmpty(dbPayload.Payload))
                return;

            dbPayload.Payload = JsonConvert.SerializeObject(payload);

            context.LivechatChannelUser.Update(dbPayload);

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// It returns a customer payload given its channel id
        /// </summary>
        /// <param name="channelId">Channel id to look for</param>
        /// <param name="exceptUserId">Excludes this user</param>
        /// <returns>Dic format Payload</returns>
        public async Task<Dictionary<string, object>> GetUserPayload(string channelId, string exceptUserId)
        {
            var user = await context.LivechatChannelUser
                .Where(t => t.LivechatUserId != exceptUserId &&
                    t.LivechatChannelId == channelId)
                .FirstOrDefaultAsync();

            if (user == null)
                return new Dictionary<string, object>();

            return JsonConvert.DeserializeObject<Dictionary<string, object>>(user.Payload);
        }

        /// <summary>
        /// Update user payload except give userid
        /// </summary>
        /// <param name="channelId">Channel to be checked</param>
        /// <param name="newPayload">New customer payload</param>
        /// <param name="exceptUserId">Update the first user inside the channel expcet by this user</param>
        /// <returns></returns>
        public async Task SetNewUserPayload(
            string channelId, 
            Dictionary<string, object> newPayload, 
            string exceptUserId)
        {
            var user = await context.LivechatChannelUser
                .Where(t => t.LivechatUserId != exceptUserId &&
                    t.LivechatChannelId == channelId)
                .FirstOrDefaultAsync();

            if (user == null)
                throw new Exception($"Customer not found. Livechat Id: {user.LivechatUserId}");

            await AddPayloadToUser(
                livechatUserId: user.LivechatUserId,
                channelId: channelId,
                payload: newPayload
            );
        }

        public async Task SetNewUserPayloadFromWidget(
            string channelId,
            Dictionary<string, object> newPayload,
            string exceptUserId)
        {
            var user = await context.LivechatChannelUser
                .Where(t => t.LivechatUserId == exceptUserId &&
                    t.LivechatChannelId == channelId)
                .FirstOrDefaultAsync();

            if (user == null)
                throw new Exception($"Customer not found. Livechat Id: {user.LivechatUserId}");

            await AddPayloadToUser(
                livechatUserId: user.LivechatUserId,
                channelId: channelId,
                payload: newPayload
            );
        }
    }
}
