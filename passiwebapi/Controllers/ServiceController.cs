using Microsoft.AspNetCore.Mvc;
using PostSharp.Extensibility;
using System;

namespace passi_webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class ServiceController : ControllerBase
    {
        [HttpGet, Route("Time")]
        public long Check()
        {
            return DateTime.UtcNow.Ticks;
        }
    }
}