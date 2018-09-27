using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovChat.Core.Logger;
using MovChat.Data.Repositories;
using MoveleirosChatServer.Models;
using System.Threading.Tasks;

namespace MoveleirosChatServer.Controllers
{
    public class StatusController : Controller
    {
        private LogRepository logRepository;

        public StatusController(UOW uow)
        {
            logRepository = uow.GetRepository<LogRepository>();
        }

        public async Task<IActionResult> Test()
        {
            await logRepository.RecLog(NopLogLevel.Info, "Livechat: healthy status called.");
            return Ok();
        }

        [Authorize]
        public async Task<IActionResult> AuthTest()
        {
            await logRepository.RecLog(NopLogLevel.Info, "Livechat: auth healthy status called.");
            return Ok();
        }

        public IActionResult TestError()
        {
            throw new System.Exception("TestError called :)");
        }

        [HttpPost]
        public async Task<IActionResult> ReportError([FromBody] ErrorMessage error)
        {
            if (string.IsNullOrEmpty(error.Message))
                return BadRequest();

            if (!RequestAllowed())
                return NotFound();

            var fullMessage = string.Empty;

            if (error.Message.Length > 30)
            {
                fullMessage = error.Message;
                error.Message = error.Message.Substring(0, 30);
            }

            await logRepository.RecLog(
                NopLogLevel.Error, 
                error.Message,
                fullMessage);

            return Ok();
        }

        private bool RequestAllowed()
        {
            var allowedHosts = "35.164.143.162";
            var remoteIp = HttpContext.Connection.RemoteIpAddress.ToString();

            if ("127.0.0.1,::1".Contains(remoteIp))
                return true;

            if (!allowedHosts.Contains(remoteIp))
                return false;

            return true;
        }
    }
}