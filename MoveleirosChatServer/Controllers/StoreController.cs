using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MovChat.Data.Repositories;
using Newtonsoft.Json;

namespace MoveleirosChatServer.Controllers
{
    [Route("[controller]")]
    public class StoreController : Controller
    {
        private readonly UOW uow;

        public StoreController(UOW uow)
        {
            this.uow = uow;
        }

        [Route("[action]/{storeId}")]
        public async Task<IActionResult> GetStoreStyle(int storeId)
        {
            if (storeId == 0)
                return BadRequest();

            var storeRepository = uow.GetRepository<StoreRepository>();
            var style = await storeRepository.GetStoreStyle(storeId);

            if (string.IsNullOrEmpty(style))
                return Ok(new { style = "" });

            var styleJson = JsonConvert.DeserializeObject(style);

            return Ok(new { style });
        }
    }
}