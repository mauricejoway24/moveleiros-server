using ImageProcessing;
using Microsoft.AspNetCore.Mvc;
using MoveleirosChatServer.Models;
using System.Threading.Tasks;

namespace MoveleirosChatServer.Controllers
{
    [Route("[controller]")]
    public class ProfileController : Controller
    {
        [Route("[action]/{width}/{height}/{name}")]
        public async Task Pic(ProfilePictureModel model)
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = 500;
                return;
            }

            var imageProcessing = new ProfileGenerator();

            var nameParse = model.Name.Split(' ');
            var firstName = nameParse[0];
            var lastName = string.Empty;

            if (nameParse.Length > 1)
                lastName = nameParse[nameParse.Length - 1];

            var pic = imageProcessing.GenerateProfile(
                firstName: firstName,
                lastName: lastName,
                width: model.Width,
                height: model.Height,
                color: model.Color
            );

            // Temporary
            var buffer = pic.GetBuffer();
            Response.ContentType = "image/jpeg";
            await Response.Body.WriteAsync(buffer, 0, buffer.Length);
            pic.Close();
        }
    }
}