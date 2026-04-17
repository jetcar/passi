using ConfigurationManager;
using Microsoft.AspNetCore.Mvc;
using Repos;
using NLog;
using Services;
using System;
using System.Threading.Tasks;
using GoogleTracer;
using Microsoft.EntityFrameworkCore;
using WebApiDto.SignUp;

namespace passi_webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Profile]
    public class SignUpController : ControllerBase
    {
        private IUserRepository _userRepository;
        private IUserService _userService;
        private ICertValidator _certValidator;
        private AppSetting _appSetting;
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public SignUpController(IUserRepository userRepository, IUserService userService, ICertValidator certValidator, AppSetting appSetting)
        {
            _userRepository = userRepository;
            _userService = userService;
            _certValidator = certValidator;
            _appSetting = appSetting;
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
                        _logger.Debug("Confirmation email flow executed for existing user.");
                    }
                    else
                    {
                        _userService.AddUserAndSendConfirmationEmail(signupDto);
                        _logger.Debug("User created and invitation email flow executed.");
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
                        _userService.SendDeleteConfirmationEmail(delete.Email);
                        _logger.Debug("Delete confirmation email dispatch attempted.");
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