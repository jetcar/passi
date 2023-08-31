using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using passi_webapi.Filters;
using PostSharp.Extensibility;
using Repos;
using Services;
using WebApiDto.SignUp;

namespace passi_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [TestFilter]
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class TestController : ControllerBase
    {

        IUserService _userService;
        private ICertValidator _certValidator;

        IUserRepository _userRepository;
        private IRandomGenerator _randomGenerator;

        public TestController(IUserRepository userRepository, IUserService userService, ICertValidator certValidator, IRandomGenerator randomGenerator)
        {
            _userRepository = userRepository;
            _userService = userService;
            _certValidator = certValidator;
            _randomGenerator = randomGenerator;
        }

        [HttpPost, Route("signup")]
        public async Task<string> SignUp([FromBody] SignupDto signupDto)
        {
            var code = "";
            var strategy = _userRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _userRepository.BeginTransaction())
                {
                    UserDb user = null;
                    if(_userRepository.IsUsernameTaken(signupDto.Email))
                        user = _userRepository.GetUser(signupDto.Email);
                    if (user == null)
                        user = _userRepository.AddUser(new UserDb()
                        {
                            Guid = signupDto.UserGuid.Value,
                            Device = new DeviceDb() { DeviceId = signupDto.DeviceId },
                            EmailHash = signupDto.Email
                        });
                    code = _randomGenerator.GetNumbersString(6);
                    var userInvitationDb = new UserInvitationDb()
                    {
                        Code = code,
                        UserId = user.Id
                    };

                    _userRepository.AddInvitation(userInvitationDb);

                    transaction.Commit();
                }
            });
            return code;
        }

        [HttpPost, Route("confirm")]
        public async Task<IActionResult> Confirm([FromBody] SignupConfirmationDto signupConfirmationDto)
        {
            var strategy = _userRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {

                using (var transaction = _userRepository.BeginTransaction())
                {
                    _certValidator.ValidateCertificate(signupConfirmationDto.PublicCert, signupConfirmationDto.Email);

                    _userService.ConfirmUser(signupConfirmationDto);
                    transaction.Commit();
                }
            });

            return Ok();
        }
    }
}
