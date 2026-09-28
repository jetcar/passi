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
            if (email == null)
                return "ok";
            var message = MailHelper.CreateSingleTemplateEmail(new EmailAddress(_appSetting["EmailFrom"]), new EmailAddress(email),
                "d-b6873d40e5c74e6bab695b5bf12a636e", new { code = code });
            var responce = await client.SendEmailAsync(message);
            if (!responce.IsSuccessStatusCode)
                throw new BadRequestException(await responce.Body.ReadAsStringAsync());
            return await responce.Body.ReadAsStringAsync();
        }

        public async Task<string> SendDeletingEmailAsync(string email, string code)
        {
            if (Convert.ToBoolean(_appSetting["DoNotSendMail"]))
                email = _appSetting["testMail"];
            if (email == null)
                return "ok";
            var message = MailHelper.CreateSingleTemplateEmail(new EmailAddress(_appSetting["EmailFrom"]), new EmailAddress(email),
                "d-b6873d40e5c74e6bab695b5bf12a636e", new { code = code });
            var responce = await client.SendEmailAsync(message);
            if (!responce.IsSuccessStatusCode)
                throw new BadRequestException(await responce.Body.ReadAsStringAsync());
            return await responce.Body.ReadAsStringAsync();
        }
    }
}