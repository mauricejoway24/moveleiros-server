using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MovChat.Core.Logger;
using MovChat.Core.Models;

namespace MovChat.Data
{
    public class LivechatContext : DbContext
    {
        private readonly IConfiguration configuration;

        public DbSet<LivechatChannel> LivechatChannel { get; set; }
        public DbSet<LivechatChannelUser> LivechatChannelUser { get; set; }
        public DbSet<LivechatMessagePack> LivechatMessagePack { get; set; }
        public DbSet<LogModel> Log { get; set; }
        public DbSet<Store> Store { get; set; }
        public DbSet<LivechatEntityAI> LivechatEntityAI { get; set; }
        public DbSet<LivechatEntityValueAI> LivechatEntityValueAI { get; set; }
        public DbSet<StoreMapping> StoreMapping { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<CustomerRoleMapping> CustomerRoleMapping { get; set; }
        public DbSet<LivechatUserToken> LivechatUserToken { get; set; }
        public DbSet<City> City { get; set; }

        public LivechatContext(IConfiguration configuration)
        {
            this.configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(configuration["ConnectionString"]);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LivechatEntityAI>()
                .HasKey(t => new { t.Id, t.StoreId });

            modelBuilder.Entity<Store>()
                .Property(t => t.IsWatson)
                .HasDefaultValue(false);

            modelBuilder.Entity<CustomerRoleMapping>()
                .HasKey(t => new { t.CustomerId, t.CustomerRoleId });
        }
    }
}
