using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostSharp.Extensibility;
using Repos;
using Services;
using System;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.AspNetCore.SignalR.Protocol;
using Serilog;
using WebApiDto.SignUp;

namespace passi_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class SignUpController : ControllerBase
    {
        private IUserRepository _userRepository;
        private IUserService _userService;
        private ICertValidator _certValidator;
        private AppSetting _appSetting;
        private ILogger _logger;

        public SignUpController(IUserRepository userRepository, IUserService userService, ICertValidator certValidator, AppSetting appSetting, ILogger logger)
        {
            _userRepository = userRepository;
            _userService = userService;
            _certValidator = certValidator;
            _appSetting = appSetting;
            _logger = logger;
        }

        [HttpPost, Route("signup")]
        public IActionResult SignUp([FromBody] SignupDto signupDto)
        {
            signupDto.Email = signupDto.Email.Trim();
            var strategy = _userRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _userRepository.BeginTransaction())
                {
                    if (_userRepository.IsUsernameTaken(signupDto.Email))
                    {
                        _userService.SendConfirmationEmail(signupDto);
                    }
                    else
                    {
                        _userService.AddUserAndSendConfirmationEmail(signupDto);
                    }

                    transaction.Commit();
                }
            });
            return Ok();
        }

        [HttpPost, Route("Code")]
        public async Task<string> Code([FromBody] SignupDto signupDto)
        {
            if (Convert.ToBoolean(_appSetting["DoNotSendMail"]))
            {
                signupDto.Email = signupDto.Email.Trim();
                var strategy = _userRepository.GetExecutionStrategy();
                _logger.Debug("returning code");
                var value = _userRepository.GetCode(signupDto.Email);
                return value;
            }

            return "test";
        }

        [HttpPost, Route("confirm")]
        public IActionResult Confirm([FromBody] SignupConfirmationDto signupConfirmationDto)
        {
            signupConfirmationDto.Email = signupConfirmationDto.Email.Trim();
            var strategy = _userRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _userRepository.BeginTransaction())
                {
                    if (!_userRepository.ValidateConfirmationCode(signupConfirmationDto.Email,
                            signupConfirmationDto.Code))
                    {
                        _userRepository.IncreaseFailedRetryCount(signupConfirmationDto.Email);
                        throw new BadRequestException("Code not found");
                    }

                    _certValidator.ValidateCertificate(signupConfirmationDto.PublicCert, signupConfirmationDto.Email);

                    _userService.ConfirmUser(signupConfirmationDto);
                    transaction.Commit();
                }
            });

            return Ok();
        }

        [HttpPost, Route("check")]
        public IActionResult Check([FromBody] SignupCheckDto confirmationDto)
        {
            confirmationDto.Email = confirmationDto.Email.Trim();
            if (!_userRepository.ValidateConfirmationCode(confirmationDto.Email, confirmationDto.Code))
                throw new BadRequestException("Code not found");

            return Ok();
        }
    }
}