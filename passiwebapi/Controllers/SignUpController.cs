using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostSharp.Extensibility;
using Repos;
using Services;
using System;
using System.Threading.Tasks;
using ConfigurationManager;
using IdentityModel;
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
                        var result = _userService.SendConfirmationEmail(signupDto);
                        _logger.Debug(result);
                    }
                    else
                    {
                        var result = _userService.AddUserAndSendConfirmationEmail(signupDto);
                        _logger.Debug(result);
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
            confirmationDto.Username = confirmationDto.Username.Trim();
            if (!_userRepository.ValidateConfirmationCode(confirmationDto.Username, confirmationDto.Code))
                throw new BadRequestException("Code not found");

            return Ok();
        }

        [HttpPost, Route("Delete")]
        public IActionResult Delete([FromBody] DeleteUserDto delete)
        {
            delete.Email = delete.Email.Trim();
            var strategy = _userRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _userRepository.BeginTransaction())
                {
                    if (_userRepository.IsUsernameTaken(delete.Email))
                    {
                        var result = _userService.SendDeleteConfirmationEmail(delete.Email);
                        _logger.Debug(result);
                    }

                    transaction.Commit();
                }
            });
            return Ok();
        }

        [HttpPost, Route("DeleteConfirmation")]
        public IActionResult DeleteConfirmation([FromBody] SignupCheckDto delete)
        {
            if (!_userRepository.ValidateConfirmationCode(delete.Username, delete.Code))
                throw new BadRequestException("Code not found");
            var strategy = _userRepository.GetExecutionStrategy();
            strategy.Execute(() =>
            {
                using (var transaction = _userRepository.BeginTransaction())
                {
                    _userService.DeleteUser(delete.Username);

                    transaction.Commit();
                }
            });
            return Ok();
        }
    }
}