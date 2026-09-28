using System;
using System.Threading.Tasks;
using WebApiDto.SignUp;

namespace Services
{
    public interface IUserService
    {
        Task<string> AddUserAndSendConfirmationEmailAsync(SignupDto signupDto);

        Guid ConfirmUser(SignupConfirmationDto signupConfirmationDto);

        Task<string> SendConfirmationEmailAsync(SignupDto signupDto);

        /// <summary>Persists a new delete-confirmation code for an existing user in the DB. Does NOT send email.</summary>
        string PrepareDeleteCode(string email);

        Task<string> SendDeleteEmailAsync(string email, string code);

        void DeleteUser(string deleteEmail);

        /// <summary>Persists a new user + invitation code in the DB. Does NOT send email.</summary>
        string PrepareNewUserSignupCode(SignupDto signupDto);

        /// <summary>Persists a new invitation code for an existing user in the DB. Does NOT send email.</summary>
        string PrepareSignupCode(SignupDto signupDto);

        /// <summary>Sends the invitation email. Safe to call from a background thread.</summary>
        Task SendSignupEmailAsync(string email, string code);
    }
}