using Models;
using Repos;
using WebApiDto.SignUp;

namespace Services
{
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

        public void AddUserAndSendConfirmationEmail(SignupDto signupDto)
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
            _emailSender.SendInvitationEmail(signupDto.Email, userInvitationDb.Code);
        }

        public void ConfirmUser(SignupConfirmationDto signupConfirmationDto)
        {
            _userRepository.ConfirmInvitation(signupConfirmationDto.Email, signupConfirmationDto.PublicCert,
                signupConfirmationDto.Guid, signupConfirmationDto.Code, signupConfirmationDto.DeviceId);
        }

        public void SendConfirmationEmail(SignupDto signupDto)
        {
            var user = _userRepository.GetUser(signupDto.Email);
            var userInvitationDb = new UserInvitationDb()
            {
                Code = _randomGenerator.GetNumbersString(6),
                UserId = user.Id
            };

            _userRepository.AddInvitation(userInvitationDb);
            _emailSender.SendInvitationEmail(signupDto.Email, userInvitationDb.Code);
        }
    }
}