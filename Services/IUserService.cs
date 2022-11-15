using WebApiDto.SignUp;

namespace Services
{
    public interface IUserService
    {
        void AddUserAndSendConfirmationEmail(SignupDto signupDto);
        void ConfirmUser(SignupConfirmationDto signupConfirmationDto);
        void SendConfirmationEmail(SignupDto signupDto);
    }
}