using WebApiDto.SignUp;

namespace Services
{
    public interface IUserService
    {
        string AddUserAndSendConfirmationEmail(SignupDto signupDto);

        void ConfirmUser(SignupConfirmationDto signupConfirmationDto);

        string SendConfirmationEmail(SignupDto signupDto);
    }
}