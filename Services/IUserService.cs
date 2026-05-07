using WebApiDto.SignUp;

namespace Services
{
    public interface IUserService
    {
        string AddUserAndSendConfirmationEmail(SignupDto signupDto);

        void ConfirmUser(SignupConfirmationDto signupConfirmationDto);

        string SendConfirmationEmail(SignupDto signupDto);

        string SendDeleteConfirmationEmail(string email);

        void DeleteUser(string deleteEmail);

        /// <summary>Persists a new user + invitation code in the DB. Does NOT send email.</summary>
        string PrepareNewUserSignupCode(SignupDto signupDto);

        /// <summary>Persists a new invitation code for an existing user in the DB. Does NOT send email.</summary>
        string PrepareSignupCode(SignupDto signupDto);

        /// <summary>Sends the invitation email. Safe to call from a background thread.</summary>
        void SendSignupEmail(string email, string code);
    }
}