namespace Services
{

    public interface IEmailSender
    {
        string SendInvitationEmail(string email, string code);
    }
}