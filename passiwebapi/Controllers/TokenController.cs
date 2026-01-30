using GoogleTracer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Repos;
using WebApiDto;

namespace passi_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Profile]
    public class TokenController : ControllerBase
    {
        private IUserRepository _userRepository;

        public TokenController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost, Route("update")]
        public IActionResult UpdateToken([FromBody] DeviceTokenUpdateDto deviceTokenUpdateDto)
        {
            var strategy = _userRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _userRepository.BeginTransaction())
                {
                    _userRepository.UpdateNotificationToken(deviceTokenUpdateDto.DeviceId,
                        deviceTokenUpdateDto.Token, deviceTokenUpdateDto.Platform);
                    transaction.Commit();
                }
            });

            return Ok();
        }
    }
}