using Microsoft.AspNetCore.Mvc;

namespace passi_webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult Check()
        {
            return Ok();
        }
    }
}
