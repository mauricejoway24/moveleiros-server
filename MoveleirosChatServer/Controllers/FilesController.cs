using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace MoveleirosChatServer.Controllers
{
    [Authorize]
    public class FilesController : Controller
    {
        private readonly IHostingEnvironment appEnvironment;

#if DEBUG
        private const string BASE_URL = "http://localhost:8080/#";
#else
        private const string BASE_URL = "https://chat.moveleiros.com.br/#";
#endif

        public FilesController(IHostingEnvironment appEnvironment)
        {
            this.appEnvironment = appEnvironment;
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile files)
        {
            var claims = User.Claims;
            var livechatId = claims
                .Where(t => t.Type == "LivechatUserId")
                .FirstOrDefault()?.Value ?? "";

            if (string.IsNullOrEmpty(livechatId))
                return BadRequest();

            var allowedExtensions = ".zip|.promob|.png|.jpg|.jpeg|.pdf|.ambient3d";
            var fileExt = Path.GetExtension(files.FileName);

            if (!allowedExtensions.Contains(fileExt))
                return BadRequest();

            var filePath = appEnvironment.WebRootPath;
            var newId = Guid.NewGuid().ToString();

            // Removing illegal chars
            livechatId = Regex.Replace(livechatId, @"[/+\\><|\[\]*% =]", "_");

            livechatId = System.Net.WebUtility.UrlEncode(livechatId);

            // Dealing with the directory
            filePath = $"{filePath}{Path.DirectorySeparatorChar}{livechatId}";
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            filePath = $"{filePath}{Path.DirectorySeparatorChar}{newId}{fileExt}";

            if (files.Length > 0)
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await files.CopyToAsync(stream);
                }
            }

            return Ok(new
            {
                filePath = $"{Request.Scheme}://{Request.Host}/{livechatId}/{newId}{fileExt}",
                fileName = files.FileName
            });
        }
    }
}