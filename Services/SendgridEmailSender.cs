using ConfigurationManager;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using GoogleTracer;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Services
{
    [Profile]
    public class SendgridEmailSender : IEmailSender
    {
        private SendGridClient client;
        private AppSetting _appSetting;

        public SendgridEmailSender(AppSetting appSetting)
        {
            _appSetting = appSetting;
            var apiKey = _appSetting["SendgridApiKey"];
            if (!Convert.ToBoolean(_appSetting["DoNotSendMail"]) || !string.IsNullOrEmpty(apiKey))
                this.client = new SendGridClient(new HttpClient(new HttpClientHandler()),
                    apiKey);
        }

        public async Task<string> SendInvitationEmailAsync(string email, string code)
        {
            if (Convert.ToBoolean(_appSetting["DoNotSendMail"]))
                email = _appSetting["testMail"];
            if (email == null || client == null)
                return "ok";
            var message = CreateMessage(_appSetting["EmailFrom"], email, code);
            var responce = await client.SendEmailAsync(message);
            if (!responce.IsSuccessStatusCode)
                throw new BadRequestException(await responce.Body.ReadAsStringAsync());
            return await responce.Body.ReadAsStringAsync();
        }

        public async Task<string> SendDeletingEmailAsync(string email, string code)
        {
            if (Convert.ToBoolean(_appSetting["DoNotSendMail"]))
                email = _appSetting["testMail"];
            if (email == null || client == null)
                return "ok";
            var message = CreateMessage(_appSetting["EmailFrom"], email, code);
            var responce = await client.SendEmailAsync(message);
            if (!responce.IsSuccessStatusCode)
                throw new BadRequestException(await responce.Body.ReadAsStringAsync());
            return await responce.Body.ReadAsStringAsync();
        }

        internal static SendGridMessage CreateMessage(string from, string to, string code)
        {
            var content = PassiEmailTemplate.CreateConfirmationCode(code);
            return MailHelper.CreateSingleEmail(
                new EmailAddress(from),
                new EmailAddress(to),
                content.Subject,
                content.PlainText,
                content.Html);
        }
    }
}
