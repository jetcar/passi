using System;
using Microsoft.AspNetCore.Mvc;

namespace passi_webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        [HttpGet, Route("Time")]
        public long Check()
        {
            return DateTime.UtcNow.Ticks;
        }

       
    }
}
