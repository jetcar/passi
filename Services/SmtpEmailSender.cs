using ConfigurationManager;
using System;
using System.Net;
using GoogleTracer;
using System.Net.Mail;
using System.Threading;
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
            var boolean = Convert.ToBoolean(_appSetting["DoNotSendMail"]);
            if (!boolean || (!string.IsNullOrEmpty(host) && host != "-"))
            {
                var port = Convert.ToInt32(_appSetting["smtpPort"]);
                var userName = _appSetting["smtpUsername"];
                var password = _appSetting["smtpPassword"];
                this.client = new SmtpClient() { Host = host, Port = port, EnableSsl = true, Credentials = new NetworkCredential(userName, password) };
            }
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
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    client.Send(message);
                    return "ok";
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message);
                    Thread.Sleep(1000);
                }

            }

            return "failed";
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