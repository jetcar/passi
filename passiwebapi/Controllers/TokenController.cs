using Microsoft.AspNetCore.Mvc;
using Repos;
using Services;
using WebApiDto;

namespace passi_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
      
        IUserRepository _userRepository;
        IUserService _userService;
        public TokenController(IUserRepository userRepository, IUserService userService)
        {
            _userRepository = userRepository;
            _userService = userService;
        }

        [HttpPost, Route("update")]
        public IActionResult UpdateToken([FromBody] DeviceTokenUpdateDto deviceTokenUpdateDto)
        {
            using (var transaction = _userRepository.BeginTransaction())
            {
                _userRepository.UpdateNotificationToken(deviceTokenUpdateDto.DeviceId,
                    deviceTokenUpdateDto.Token, deviceTokenUpdateDto.Platform);
                transaction.Commit();
            }

            return Ok();
        }
    }
}
