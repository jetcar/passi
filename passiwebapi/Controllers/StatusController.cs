using Microsoft.AspNetCore.Mvc;
using PostSharp.Extensibility;

namespace passi_webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult Check()
        {
            return Ok();
        }
    }
}