using System.Threading.Tasks;

namespace Services
{
    public interface IEmailSender
    {
        Task<string> SendInvitationEmailAsync(string email, string code);

        Task<string> SendDeletingEmailAsync(string email, string code);
    }
}