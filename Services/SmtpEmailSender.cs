using ConfigurationManager;
using System;
using System.Net;
using GoogleTracer;
using System.Net.Mail;
using Serilog;

namespace Services
{
    [Profile]
    public class SmtpEmailSender : IEmailSender
    {
        private SmtpClient client;
        private AppSetting _appSetting;
        private readonly ILogger _logger;

        public SmtpEmailSender(AppSetting appSetting, ILogger logger)
        {
            _appSetting = appSetting;
            _logger = logger;
            var host = _appSetting["smtpHost"];
            if (!Convert.ToBoolean(_appSetting["DoNotSendMail"]) || !string.IsNullOrEmpty(host))
                this.client = new SmtpClient() { Host = host, Port = 465, EnableSsl = true, Credentials = new NetworkCredential(_appSetting["smtpUsername"], _appSetting["smtpPassword"]) };
        }

        public string SendInvitationEmail(string email, string code)
        {
            if (Convert.ToBoolean(_appSetting["DoNotSendMail"]))
                email = _appSetting["testMail"];
            if (email == null)
                return "ok";
            var message = new MailMessage(_appSetting["smtpUsername"], email)
            {
                IsBodyHtml = true,
                Subject = $"Passi code {code}",
                Body = "<html><body><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" >" +
                "<tr><td>Enter this code in application.</td></tr>" +
                $"<tr><td><b>{code}</b></td></tr>" +
                "</table></body></html>"
            };
            try
            {
                client.Send(message);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return "failed";
            }
            return "ok";
        }

        public string SendDeletingEmail(string email, string code)
        {
            if (Convert.ToBoolean(_appSetting["DoNotSendMail"]))
                email = _appSetting["testMail"];
            if (email == null)
                return "ok";
            var message = new MailMessage(_appSetting["smtpUsername"], email)
            {
                IsBodyHtml = true,
                Subject = $"Passi code {code}",
                Body = "<html><body><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" >" +
                "<tr><td>Enter this code in application.</td></tr>" +
                $"<tr><td><b>{code}</b></td></tr>" +
                "</table></body></html>"
            };
            try
            {
                client.Send(message);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
                return "failed";
            }
            return "ok";
        }
    }
}