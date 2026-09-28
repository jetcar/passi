using System;
using System.Threading.Tasks;
using GoogleTracer;
using Models;

using Repos;
using WebApiDto.SignUp;

namespace Services
{
    [Profile]
    public class UserService : IUserService
    {
        private IUserRepository _userRepository;
        private IRandomGenerator _randomGenerator;
        private IEmailSender _emailSender;

        public UserService(IUserRepository userRepository, IRandomGenerator randomGenerator, IEmailSender emailSender)
        {
            _userRepository = userRepository;
            _randomGenerator = randomGenerator;
            _emailSender = emailSender;
        }

        public Task<string> AddUserAndSendConfirmationEmailAsync(SignupDto signupDto)
        {
            var code = PrepareNewUserSignupCode(signupDto);
            return _emailSender.SendInvitationEmailAsync(signupDto.Email, code);
        }

        public string PrepareNewUserSignupCode(SignupDto signupDto)
        {
            var userInvitationDb = new UserInvitationDb()
            {
                Code = _randomGenerator.GetNumbersString(6),
            };
            var user = new UserDb()
            {
                EmailHash = signupDto.Email,

                Guid = signupDto.UserGuid.Value,
                Device = new DeviceDb()
                {
                    DeviceId = signupDto.DeviceId
                }
            };
            user.Invitations.Add(userInvitationDb);
            _userRepository.AddUser(user);
            return userInvitationDb.Code;
        }

        public Guid ConfirmUser(SignupConfirmationDto signupConfirmationDto)
        {
            return _userRepository.ConfirmInvitation(signupConfirmationDto.Email, signupConfirmationDto.PublicCert,
                signupConfirmationDto.Guid, signupConfirmationDto.Code, signupConfirmationDto.DeviceId);
        }

        public Task<string> SendConfirmationEmailAsync(SignupDto signupDto)
        {
            var code = PrepareSignupCode(signupDto);
            return _emailSender.SendInvitationEmailAsync(signupDto.Email, code);
        }

        public string PrepareSignupCode(SignupDto signupDto)
        {
            var user = _userRepository.GetUser(signupDto.Email);
            var userInvitationDb = new UserInvitationDb()
            {
                Code = _randomGenerator.GetNumbersString(6),
                UserId = user.Id
            };

            _userRepository.AddInvitation(userInvitationDb);
            return userInvitationDb.Code;
        }

        public Task SendSignupEmailAsync(string email, string code)
        {
            return _emailSender.SendInvitationEmailAsync(email, code);
        }

        public string PrepareDeleteCode(string email)
        {
            var user = _userRepository.GetUser(email);
            var userInvitationDb = new UserInvitationDb()
            {
                Code = _randomGenerator.GetNumbersString(6),
                UserId = user.Id,
                Delete = true
            };

            _userRepository.AddInvitation(userInvitationDb);
            return userInvitationDb.Code;
        }

        public Task<string> SendDeleteEmailAsync(string email, string code)
        {
            return _emailSender.SendDeletingEmailAsync(email, code);
        }

        public void DeleteUser(string deleteEmail)
        {
            var user = _userRepository.GetUser(deleteEmail);

            _userRepository.DeleteAccount(user.Guid);
        }
    }
}