using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovChat.Core.Hub;
using MovChat.Core.Models;
using MovChat.Data.Repositories;

namespace MoveleirosChatServer.Controllers
{
    // TODO: Include Authorize Attribute
    // Needs to include Bearer header on Java/Android request object
    [Authorize]
    public class TokenController : Controller
    {
        private readonly UOW uow;

        public TokenController(UOW uow)
        {
            this.uow = uow;
        }

        [HttpPost]
        public async Task<IActionResult> RegisterToken([FromBody] TokenDetail token)
        {
            // Persists on database
            // TODO: Get AuthToken from query/header instead of param
            var user = HttpContext.User;

            var livechatUserToken = uow.GetRepository<LivechatUserTokenRepository>();
            await livechatUserToken.PersistTokenDetail(new LivechatUserToken
            {
                AuthToken = user.FindFirstValue("AuthToken") ?? "",
                PushToken = token.Token,
                Device = token.Device,
                Version = token.Version,
                LivechatUserId = user.FindFirstValue("LivechatUserId") ?? "",
                Role = user.FindFirstValue(ClaimTypes.Role) ?? "",
                Stores = user.FindFirstValue("Stores") ?? ""
            });

            return Ok();
        }

        public async Task<IActionResult> UnRegisterToken([FromBody] TokenDetail token)
        {
            var livechatUserToken = uow.GetRepository<LivechatUserTokenRepository>();

            await livechatUserToken.RemoveTokenDetail(new LivechatUserToken
            {
                AuthToken = token.AuthToken,
                PushToken = token.Token,
                Device = token.Device,
                Version = token.Version,
                LivechatUserId = token.LivechatUserId
            });

            return Ok();
        }
    }
}