using Microsoft.AspNetCore.Mvc;
using PostSharp.Extensibility;

namespace WebApp.Controllers
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