using System.Threading.Tasks;
using Dapper;
using System;
using System.Collections.Generic;
using MoveleirosChatServer.Models;
using System.Linq;
using MoveleirosChatServer.Utils;
using Newtonsoft.Json;
using MovChat.Core.Models;
using MovChat.Core.UI;

namespace MoveleirosChatServer.Data
{
    public class LivechatRules
    {
        private SQLFactory sqlFactory;

        public LivechatRules(SQLFactory sqlFactory)
        {
            this.sqlFactory = sqlFactory;
        }

        public async Task CreateAndSave(LivechatUser livechatUser)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                string insertStatement;

                if (livechatUser.CustomerId.HasValue)
                    insertStatement = $"insert {nameof(LivechatUser)}(Id, CustomerId, Name, Email, Phone) values (@Id, @CustomerId, @Name, @Email, @Phone)";
                else
                    insertStatement = $"insert {nameof(LivechatUser)}(Id, Name, Email, Phone) values (@Id, @Name, @Email, @Phone)";

                await db.ExecuteAsync(
                    insertStatement,
                    new
                    {
                        Id = livechatUser.Id,
                        CustomerId = livechatUser.CustomerId,
                        Name = livechatUser.Name,
                        Email = livechatUser.Email,
                        Phone = livechatUser.Phone
                    }
                );
            }
        }

        public async Task<int> CreateAndGetCustomer(string name, string uniqueIdentifier, string phone)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var dbCustomerId = await db.QueryFirstOrDefaultAsync<int>("select Id from Customer where " +
                    "UniqueCookieIdentifier = @UniqueIdentifier", new { UniqueIdentifier = uniqueIdentifier });

                if (dbCustomerId > 0)
                    return dbCustomerId;

                var insertStatement = $"insert Customer(CustomerGuid, Phone, Active, Deleted, IsTaxExempt," +
                    $"AffiliateId, VendorId, HasShoppingCartItems, RequireReLogin, FailedLoginAttempts, IsSystemAccount," +
                    $"CreatedOnUtc, LastActivityDateUtc, RegisteredInStoreId, UniqueCookieIdentifier) values (@CustomerGuid, @Phone, 1, 0, 1," +
                    $"0, 0, 0, 0, 0, 0, @CreatedAt, @LastActive, 1, @UniqueIdentifier);" +
                    $"" +
                    $"select @@IDENTITY;";

                var id = await db.ExecuteScalarAsync(
                    insertStatement,
                    new
                    {
                        CustomerGuid = Guid.NewGuid().ToString().ToUpper(),
                        Phone = phone,
                        CreatedAt = DateTime.Now,
                        LastActive = DateTime.Now,
                        UniqueIdentifier = uniqueIdentifier
                    }
                );

                var idParsed = Convert.ToInt32(id);

                // await db.ExecuteAsync(@"update Customer set Email = @Email where Id = @Id", new { Id = idParsed, Email = $"{idParsed}@chat.com.br" });

                await db.ExecuteAsync(@"insert GenericAttribute(EntityId, KeyGroup, [Key], [Value], StoreId) values " +
                    "(@EntityId, @KeyGroup, @Key, @Value, 0);",
                    new
                    {
                        EntityId = idParsed,
                        KeyGroup = "Customer",
                        Key = "FirstName",
                        Value = name
                    });

                var idRoleCustomer = await db.QueryFirstOrDefaultAsync<int>(@"select Id from CustomerRole where SystemName = 'LivechatCustomer';");

                if (idRoleCustomer == 0)
                    throw new Exception("Customer role cannot be null inside CreateAndGetCustomer method");

                await db.ExecuteAsync(@"insert Customer_CustomerRole_Mapping (Customer_Id, CustomerRole_Id) values (@Id, @Role);",
                    new[]
                    {
                        new { Id = idParsed, Role = idRoleCustomer },
                        new { Id = idParsed, Role = 3 },
                    });

                return idParsed;
            }
        }

        public async Task CloneUserToAStore(int? customerId, int customerStoreId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var uniqueIdentifier = await db.QueryFirstOrDefaultAsync<string>("select UniqueCookieIdentifier from Customer where Id = @Id", 
                    new { Id = customerId });

                var dbCustomerId = await db.QueryFirstOrDefaultAsync<int>("select Id from Customer where " +
                    "UniqueCookieIdentifier = @UniqueCookieIdentifier and RegisteredInStoreId = @RegisteredInStoreId", 
                    new { UniqueCookieIdentifier = uniqueIdentifier, RegisteredInStoreId = customerStoreId });

                if (dbCustomerId > 0 || !customerId.HasValue)
                    return;

                var cloneStatement = $"insert Customer(CustomerGuid, Active, Deleted, IsTaxExempt," +
                    $"AffiliateId, VendorId, HasShoppingCartItems, RequireReLogin, FailedLoginAttempts, IsSystemAccount," +
                    $"CreatedOnUtc, LastActivityDateUtc, RegisteredInStoreId, UniqueCookieIdentifier) " +
                    $"" +
                    $"select @CustomerGuid, 1, 0, IsTaxExempt," +
                    $"AffiliateId, VendorId, HasShoppingCartItems, RequireReLogin, FailedLoginAttempts, IsSystemAccount," +
                    $"@CreatedAt, @LastActive, @RegisteredInStoreId, UniqueCookieIdentifier from " +
                    $"Customer where Id = @Id;" +
                    $"Select @@IDENTITY;";

                // CLONE CUSTOMER
                var newId = await db.ExecuteScalarAsync<int>(
                    cloneStatement,
                    new
                    {
                        Id = customerId.Value,
                        CustomerGuid = Guid.NewGuid().ToString().ToUpper(),
                        RegisteredInStoreId = customerStoreId,
                        CreatedAt = DateTime.Now,
                        LastActive = DateTime.Now,
                    });

                // CLONE NAME
                await db.ExecuteAsync(@"insert GenericAttribute(EntityId, KeyGroup, [Key], [Value], StoreId) " +
                    "select @NewEntityId, KeyGroup, [Key], [Value], StoreId from GenericAttribute where EntityId = @EntityId " +
                    "and [Key] = 'FirstName' and [KeyGroup] = 'Customer';",
                    new
                    {
                        NewEntityId = newId,
                        EntityId = customerId.Value
                    });

                var idRoleCustomer = await db.QueryFirstOrDefaultAsync<int>(@"select Id from CustomerRole where SystemName = 'LivechatCustomer';");

                if (idRoleCustomer == 0)
                    throw new Exception("Customer role cannot be null inside CloneUserToAStore method");

                // CUSTOMER ROLES
                await db.ExecuteAsync(@"insert Customer_CustomerRole_Mapping (Customer_Id, CustomerRole_Id) values (@Id, @Role);",
                    new[]
                    {
                        new { Id = newId, Role = idRoleCustomer },
                        new { Id = newId, Role = 3 },
                    });
            }
        }

        public async Task CreateNewSession(LivechatUserSession livechatSession)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                await db.ExecuteAsync(
                    $"insert {nameof(LivechatUserSession)}(LivechatUserId, ConnectionId) values (@LivechatUserId, @ConnectionId)",
                    new
                    {
                        LivechatUserId = livechatSession.LivechatUserId,
                        ConnectionId = livechatSession.ConnectionId
                    });
            }
        }

        public async Task<LivechatChannel> GetCustomerChannel(int storeId, string livechatUserId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var channel = await db.QueryFirstOrDefaultAsync<LivechatChannel>($"select A.* from {nameof(LivechatChannel)} A " +
                    $"inner join {nameof(LivechatChannelUser)} B on B.{nameof(LivechatChannelUser.LivechatChannelId)} = A.Id " +
                    $"where A.{nameof(LivechatChannel.IsFinished)} = 0 and B.{nameof(LivechatChannelUser.LivechatUserId)} = @LivechatUserId " +
                    $"and A.{nameof(LivechatChannel.StoreId)} = @StoreId",
                    new
                    {
                        LivechatUserId = livechatUserId,
                        StoreId = storeId
                    });

                return channel;
            }
        }

        public async Task<LivechatChannel> CreateChannel(int storeId, LivechatUser livechatUser)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var newChannel = new LivechatChannel
                {
                    Name = livechatUser.Name,
                    StoreId = storeId
                };

                // Creates a new channel and add user into it
                await db.ExecuteAsync(
                    $"insert {nameof(LivechatChannel)}(Id, Name, StoreId, StoreName) values (@Id, @Name, @StoreId, @StoreName);" +
                    $"insert {nameof(LivechatChannelUser)}(Id, LivechatUserId, LivechatChannelId) values (@IdCUser, @LivechatUserId, @LivechatChannelId)",
                    new
                    {
                        Id = newChannel.Id,
                        Name = newChannel.Name,
                        StoreId = newChannel.StoreId,
                        StoreName = "Sem Alias",
                        IdCUser = Guid.NewGuid().ToString(),
                        LivechatUserId = livechatUser.Id,
                        LivechatChannelId = newChannel.Id
                    });

                return newChannel;
            }
        }

        public async Task<List<LivechatMessagePack>> LoadLastMessages(LivechatUserSession livechatSession, LivechatChannel userCurrentChannel)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var messages = await db.QueryAsync<LivechatMessagePack>(
                    $"select * from {nameof(LivechatMessagePack)} where ChannelId = @ChannelId order by CreatedAt desc",
                    new
                    {
                        ChannelId = userCurrentChannel.ChannelId
                    });

                var result = messages.AsList();

                foreach (var message in result)
                {
                    if (!string.IsNullOrEmpty(message.SerializedElements))
                        message.Elements = JsonConvert.DeserializeObject<List<ElementUI>>(message.SerializedElements);
                }

                result.Reverse();

                return result;
            }
        }

        public async Task<Customer> FindUserByPassword(LoginUserModel model)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var data = await db.QueryAsync<Customer, CustomerPassword, Customer>($"select * from {nameof(Customer)} A " +
                    $"inner join {nameof(CustomerPassword)} B on B.CustomerId = A.Id " +
                    $"where A.Deleted = 0 and A.Active = 1 and A.Email = @Email " +
                    $"order by B.CreatedOnUtc desc",
                    map: (cus, cusPassword) =>
                    {
                        cus.CustomerPassword = cusPassword;

                        return cus;
                    },
                    param: new
                    {
                        Email = model.Email
                    });

                var customer = data.FirstOrDefault();

                /*if (customer != null)
                {
                    if (!PasswordHash.PasswordsMatch(
                        customer.CustomerPassword.Password, 
                        model.Password, 
                        customer.CustomerPassword.PasswordSalt))
                    {
                        customer = null;
                    }
                }*/

                return customer;
            }
        }

        public async Task<bool> UserHasLivechatPermission(int customerId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var permissions = await db.QueryAsync(
                    $"select A.* from Customer_CustomerRole_Mapping A " +
                    $"inner join Customer C on C.Id = A.Customer_Id " +
                    $"inner join [CustomerRole] CR on CR.Id = A.[CustomerRole_Id]" +
                    $"where A.Customer_Id = @CustomerId and CR.[SystemName] = 'LivechatAgent'",
                    new
                    {
                        CustomerId = customerId
                    });

                return permissions.Count() > 0;
            }
        }

        public async Task<LivechatUser> FindLivechatUserByCustomerId(int customerId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var livechatUser = await db.QueryFirstOrDefaultAsync<LivechatUser>(
                    $"select A.* from {nameof(LivechatUser)} A " +
                    $"where A.CustomerId = @CustomerId",
                    new
                    {
                        CustomerId = customerId
                    });

                return livechatUser;
            }
        }

        public async Task<string> GetCustomerNameFromNopAttributes(int customerId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var customerName = await db.QueryFirstOrDefaultAsync<string>(
                    $"select A.Value from GenericAttribute A " +
                    $"where A.KeyGroup = @KeyGroup and A.[Key] = @Key and A.EntityId = @Id",
                    new
                    {
                        KeyGroup = "Customer",
                        Key = "FirstName",
                        Id = customerId
                    });

                return customerName;
            }
        }

        public async Task<IEnumerable<string>> GetStoresByCustomerId(int customerId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var stores = await db.QueryAsync<string>(
                    $"select StoreId from StoreMapping " +
                    $"where EntityName = 'Stores' and EntityId = @CustomerId",
                    new
                    {
                        CustomerId = customerId
                    });

                return stores;
            }
        }

        public async Task<List<LivechatChannel>> GetAgentChannels(string livechatUserId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var channels = await db.QueryAsync<LivechatChannel>(
                    $"select A.* from {nameof(LivechatChannel)} A " +
                    $"inner join {nameof(LivechatChannelUser)} B on B.{nameof(LivechatChannelUser.LivechatChannelId)} = A.{nameof(LivechatChannel.Id)} " +
                    $"where B.{nameof(LivechatChannelUser.LivechatUserId)} = @LivechatUserId and A.{nameof(LivechatChannel.IsFinished)} = 0 " +
                    $"order by A.{nameof(LivechatChannel.CreatedAt)} desc",
                    new
                    {
                        LivechatUserId = livechatUserId
                    });

                return channels.AsList();
            }
        }

        public async Task<bool> IsCustomerAloneAtRoom(string channelId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var people = await db.QueryAsync(
                    $"select 1 from {nameof(LivechatChannelUser)} " +
                    $"where {nameof(LivechatChannelUser.LivechatChannelId)} = @ChannelId",
                    new
                    {
                        ChannelId = channelId
                    });

                return people.Count() <= 1;
            }
        }

        public async Task<LivechatChannelUser> GetAgentsOnChannels(string channelId, string currentUserId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var agent = await db.QueryFirstOrDefaultAsync<LivechatChannelUser>(
                    $"select * from {nameof(LivechatChannelUser)} " +
                    $"where {nameof(LivechatChannelUser.LivechatChannelId)} = @ChannelId and " +
                    $"{nameof(LivechatChannelUser.LivechatUserId)} <> @CurrentUserId",
                    new
                    {
                        ChannelId = channelId,
                        CurrentUserId = currentUserId
                    });

                return agent;
            }
        }

        public async Task AddUserToChannel(LivechatChannelUser livechatChannelUser)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                await db.ExecuteAsync(
                    $"insert {nameof(LivechatChannelUser)} ({nameof(LivechatChannelUser.Id)}, " +
                        $"{nameof(LivechatChannelUser.LivechatUserId)}, " +
                        $"{nameof(LivechatChannelUser.LivechatChannelId)}) " +
                    $"values (@Id, @LivechatUserId, @LivechatChannelId)",
                    new
                    {
                        Id = livechatChannelUser.Id,
                        LivechatUserId = livechatChannelUser.LivechatUserId,
                        LivechatChannelId = livechatChannelUser.LivechatChannelId
                    });
            }
        }

        public async Task<LivechatChannel> GetCurrentUserChannel(string livechatUserId, string channelId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var channel = await db.QueryFirstOrDefaultAsync<LivechatChannel>(
                    $"select A.* from {nameof(LivechatChannel)} A " +
                    $"inner join {nameof(LivechatChannelUser)} B on B.{nameof(LivechatChannelUser.LivechatChannelId)} = A.{nameof(LivechatChannel.Id)} " +
                    $"where B.{nameof(LivechatChannelUser.LivechatUserId)} = @LivechatUserId and A.{nameof(LivechatChannel.IsFinished)} = 0 " +
                    $"and A.{nameof(LivechatChannel.Id)} = @Id " +
                    $"order by A.{nameof(LivechatChannel.CreatedAt)} desc",
                    new
                    {
                        Id = channelId,
                        LivechatUserId = livechatUserId
                    });

                return channel;
            }
        }

        public async Task<LivechatMessagePack> GetLastMessage(string channelId)
        {
            using (var db = sqlFactory.GetNewConnection())
            {
                var message = await db.QueryFirstOrDefaultAsync<LivechatMessagePack>(
                    $"select top 1 * from {nameof(LivechatMessagePack)} A " +
                    $"where A.{nameof(LivechatMessagePack.ChannelId)} = @ChannelId " +
                    $"order by A.{nameof(LivechatMessagePack.CreatedAt)} desc",
                    new
                    {
                        ChannelId = channelId
                    });

                return message;
            }
        }
    }
}
