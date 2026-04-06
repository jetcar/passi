using ConfigurationManager;
using System;
using System.Net;
using GoogleTracer;
using System.Net.Mail;
using System.Threading;
using log4net;

namespace Services
{
    [Profile]
    public class SmtpEmailSender : IEmailSender
    {
        private const int MaxRetryAttempts = 10;
        private const int RetryDelayMilliseconds = 1000;
        private const string SuccessResult = "ok";
        private const string FailureResult = "failed";

        private SmtpClient client;
        private AppSetting _appSetting;
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SmtpEmailSender));

        public SmtpEmailSender(AppSetting appSetting)
        {
            _appSetting = appSetting;
            var host = _appSetting["smtpHost"];
            if (!string.IsNullOrEmpty(host) && host != "-")
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
                return SuccessResult;
            if (client == null)
            {
                _logger.Error("SMTP client is not configured. Set smtpHost to send emails.");
                return FailureResult;
            }
            var emailFrom = _appSetting["emailFrom"];
            var message = new MailMessage(emailFrom, email)
            {
                IsBodyHtml = true,
                Subject = $"Passi code {code}",
                Body = "<html><body><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" >" +
                "<tr><td>Enter this code in application.</td></tr>" +
                $"<tr><td><b>{code}</b></td></tr>" +
                "</table></body></html>"
            };
            for (int i = 0; i < MaxRetryAttempts; i++)
            {
                try
                {
                    client.Send(message);
                    return SuccessResult;
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, e);
                    Thread.Sleep(RetryDelayMilliseconds);
                }

            }

            return FailureResult;
        }

        public string SendDeletingEmail(string email, string code)
        {
            if (Convert.ToBoolean(_appSetting["DoNotSendMail"]))
                email = _appSetting["testMail"];
            if (email == null)
                return SuccessResult;
            if (client == null)
            {
                _logger.Error("SMTP client is not configured. Set smtpHost to send emails.");
                return FailureResult;
            }
            var senderEmail = _appSetting["smtpUsername"];
            var message = new MailMessage(senderEmail, email)
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
                _logger.Error(e.Message, e);
                return FailureResult;
            }
            return SuccessResult;
        }
    }
}