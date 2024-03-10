using Microsoft.AspNetCore.Mvc;

using System;
using GoogleTracer;

namespace passi_webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Profile]
    public class ServiceController : ControllerBase
    {
        [HttpGet, Route("Time")]
        public long Check()
        {
            return DateTime.UtcNow.Ticks;
        }
    }
}