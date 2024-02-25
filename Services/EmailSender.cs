using System;
using System.Net.Http;
using ConfigurationManager;
using PostSharp.Extensibility;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Services
{
    [Profile(AttributeTargetElements = MulticastTargets.Method)]
    public class EmailSender : IEmailSender
    {
        private SendGridClient client;
        private AppSetting _appSetting;

        public EmailSender(AppSetting appSetting)
        {
            _appSetting = appSetting;
            var apiKey = _appSetting["SendgridApiKey"];
            if (!Convert.ToBoolean(_appSetting["DoNotSendMail"]) || !string.IsNullOrEmpty(apiKey))
                this.client = new SendGridClient(new HttpClient(new HttpClientHandler()),
                    apiKey);
        }

        public string SendInvitationEmail(string email, string code)
        {
            if (Convert.ToBoolean(_appSetting["DoNotSendMail"]))
                email = _appSetting["testMail"];
            if (email == null)
                return "ok";
            var message = MailHelper.CreateSingleTemplateEmail(new EmailAddress(_appSetting["EmailFrom"]), new EmailAddress(email),
                "d-b6873d40e5c74e6bab695b5bf12a636e", new { code = code });
            var responce = client.SendEmailAsync(message).Result;
            if (!responce.IsSuccessStatusCode)
                throw new BadRequestException(responce.Body.ReadAsStringAsync().Result);
            return responce.Body.ReadAsStringAsync().Result;
        }

        public string SendDeletingEmail(string email, string code)
        {
            if (Convert.ToBoolean(_appSetting["DoNotSendMail"]))
                email = _appSetting["testMail"];
            if (email == null)
                return "ok";
            var message = MailHelper.CreateSingleTemplateEmail(new EmailAddress(_appSetting["EmailFrom"]), new EmailAddress(email),
                "d-b6873d40e5c74e6bab695b5bf12a636e", new { code = code });
            var responce = client.SendEmailAsync(message).Result;
            if (!responce.IsSuccessStatusCode)
                throw new BadRequestException(responce.Body.ReadAsStringAsync().Result);
            return responce.Body.ReadAsStringAsync().Result;
        }
    }
}