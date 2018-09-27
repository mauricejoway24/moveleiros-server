using Microsoft.EntityFrameworkCore;
using MovChat.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovChat.Data.Repositories
{
    public class ChannelRepository : IRepositoryBase
    {
        private readonly LivechatContext context;

        public ChannelRepository(LivechatContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<LivechatChannel>> GetAgentChannels(string livechatUserId)
        {
            var channels = await context.LivechatChannel
                .Where(t => t.Users.Any(u => u.LivechatUserId == livechatUserId) && !t.IsFinished)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return channels;
        }

        public async Task<List<LivechatChannelUser>> GetAgentsOnChannel(string channelId, string exceptThisLivechatUserId)
        {
            var agents = await context.LivechatChannelUser
                .Where(t => 
                    t.LivechatChannelId == channelId && 
                    t.LivechatUserId != exceptThisLivechatUserId)
                .ToListAsync();

            return agents;
        }

        public async Task EndChat(string channelId)
        {
            var channel = await context.LivechatChannel
                .Where(t => t.Id == channelId)
                .FirstOrDefaultAsync();

            if (channel == null)
                return;

            channel.IsFinished = true;

            context.LivechatChannel.Update(channel);
            await context.SaveChangesAsync();
        }

        public Task<bool> ChannelExists(string channelId)
        {
            return context.LivechatChannel.AnyAsync(t => t.Id == channelId);
        }
    }
}
