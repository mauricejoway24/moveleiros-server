using JWT;
using Microsoft.AspNetCore.Mvc;
using MovChat.Core.Models;
using MovChat.Data.Repositories;
using MoveleirosChatServer.Auth;
using MoveleirosChatServer.Data;
using MoveleirosChatServer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MoveleirosChatServer.Controllers
{
    public class LivechatController : Controller
    {
        private LivechatRules livechatRules;
        private readonly UOW uow;
        private readonly LogRepository logRepository;

        public LivechatController(
            LivechatRules livechatRules,
            UOW uow)
        {
            this.livechatRules = livechatRules;
            this.uow = uow;

            logRepository = uow.GetRepository<LogRepository>();
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterNewUserModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var token = GenerateToken(model.Name);
            var uniqueIdentifier = GenerateIdentifier(model);

            var customer = await livechatRules.CreateAndGetCustomer(model.Name, uniqueIdentifier, phone: model.Phone);

            await livechatRules.CreateAndSave(new LivechatUser
            {
                Id = token,
                Email = model.Email,
                Name = model.Name,
                Phone = model.Phone,
                CustomerId = customer
            });

            return SetClaimAndSignIn(
                model.Name, 
                model.Email, 
                "LivechatCustomer", 
                token, 
                new string[] {},
                model.Payload,
                customerId: customer
            );
        }

        private string GenerateIdentifier(RegisterNewUserModel model)
        {
            IEnumerable<byte> uniqueConcat = new byte[0];

            if (!string.IsNullOrEmpty(model.Name))
            {
                byte[] name = Encoding.ASCII.GetBytes(model.Name);
                uniqueConcat = uniqueConcat.Concat(name);
            }

            if (!string.IsNullOrEmpty(model.Phone))
            {
                byte[] phone = Encoding.ASCII.GetBytes(model.Phone);
                uniqueConcat = uniqueConcat.Concat(phone);
            }

            if (!string.IsNullOrEmpty(model.Email))
            {
                byte[] email = Encoding.ASCII.GetBytes(model.Email);
                uniqueConcat = uniqueConcat.Concat(email);
            }

            if (model.Payload != null && model.Payload.Count > 0)
            {
                var payload = JsonConvert.SerializeObject(model.Payload);
                var payloadBytes = Encoding.ASCII.GetBytes(payload);
                uniqueConcat.Concat(payloadBytes);
            }

            if (uniqueConcat == null)
                uniqueConcat = Guid.NewGuid().ToByteArray();

            return Convert.ToBase64String(uniqueConcat.ToArray());
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginUserModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check in the NopCommerce if user exists
            var user = await livechatRules.FindUserByPassword(model);

            if (user == null)
            {
                await logRepository.RecAuthLog($"Invalid chat login attempt using {model.Email}.");

                return Unauthorized();
            }

            var hasPermission = await livechatRules.UserHasLivechatPermission(user.Id);

            if (!hasPermission)
            {
                await logRepository.RecAuthLog($"User {model.Email} doesn't have access to this chat as agent.");

                return Unauthorized();
            }

            // Check if user exists inside LivechatUser table
            var livechatUser = await livechatRules.FindLivechatUserByCustomerId(user.Id);

            // When not found, creates a new register inside LivechatUser table
            if (livechatUser == null)
            {
                // Get customer's name
                var customerName = await livechatRules.GetCustomerNameFromNopAttributes(user.Id);
                var newToken = GenerateToken(customerName);

                await livechatRules.CreateAndSave(new LivechatUser
                {
                    Id = newToken,
                    CustomerId = user.Id,
                    Email = user.Email,
                    Name = customerName,
                    Phone = ""
                });

                // Update livechatUser to its new attributes
                livechatUser = new LivechatUser
                {
                    Id = newToken,
                    Name = customerName
                };
            }

            var authorizedStores = await livechatRules.GetStoresByCustomerId(user.Id);

            if (authorizedStores.Count() == 0)
            {
                await logRepository.RecAuthLog($"User {model.Email} is registered at any store.");

                return Unauthorized();
            }

            return SetClaimAndSignIn
            (
                livechatUser.Name,
                user.Email,
                "LivechatAgent",
                livechatUser.Id,
                authorizedStores.ToArray(),
                customerId: user.Id,
                device: model.Device,
                version: model.Version
            );
        }

        private string GenerateToken(string username)
        {
            byte[] uniqueId = Guid.NewGuid().ToByteArray();
            byte[] name = Encoding.ASCII.GetBytes(username);
            return Convert.ToBase64String(name.Concat(uniqueId).ToArray());
        }

        private IActionResult SetClaimAndSignIn(
            string username, 
            string email, 
            string role, 
            string token, 
            string[] stores,
            Dictionary<string, object> payload = null,
            int customerId = 0,
            string device = "",
            string version = "")
        {
            var jwtEncoder = new JwtMovEncoder();
            var newLivechatUserId = token;

            var provider = new UtcDateTimeProvider();
#if DEBUG
            var expirationDate = provider.GetNow().AddHours(2);
#else
            var expirationDate = provider.GetNow().AddYears(1);
#endif
            var secondsSinceEpoch = Math.Round((expirationDate - JwtValidator.UnixEpoch).TotalSeconds);

            var tokenPayload = new Dictionary<string, object>
            {
                { ClaimTypes.Name, username },
                { ClaimTypes.Email, email },
                { ClaimTypes.Role, role },
                { "CustomerId", customerId },
                { "LivechatUserId", newLivechatUserId },
                { "Device", device },
                { "Version", version },
                { "exp", secondsSinceEpoch }
            };

            if (payload != null && payload.Count > 0)
            {
                var keyValue = new Dictionary<string, object>();

                foreach (var p in payload)
                    keyValue.Add(p.Key, p.Value);

                tokenPayload.Add("UserPayload", keyValue);
            }

            if (stores != null && stores.Length > 0)
                tokenPayload.Add("Stores", string.Join(',', stores));

            var jwtToken = jwtEncoder.Encode(tokenPayload);

            return Ok(new
            {
                username,
                livechatUserId = token,
                token = jwtToken.Token
            });
        }
    }
}