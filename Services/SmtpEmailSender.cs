using ConfigurationManager;
using System;
using System.Net;
using GoogleTracer;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Services
{
    [Profile]
    public class SmtpEmailSender : IEmailSender
    {
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMilliseconds = 2000;
        private const int SmtpTimeoutMilliseconds = 15_000;
        private const string SuccessResult = "ok";
        private const string FailureResult = "failed";

        private SmtpClient client;
        private AppSetting _appSetting;
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SmtpEmailSender));

        public SmtpEmailSender(AppSetting appSetting)
        {
            _appSetting = appSetting;
            var host = _appSetting["smtpHost"];
            var boolean = Convert.ToBoolean(_appSetting["DoNotSendMail"]);
            if (!boolean || (!string.IsNullOrEmpty(host) && host != "-"))
            {
                var port = Convert.ToInt32(_appSetting["smtpPort"]);
                var userName = _appSetting["smtpUsername"];
                var password = _appSetting["smtpPassword"];
                this.client = new SmtpClient() { Host = host, Port = port, EnableSsl = !Convert.ToBoolean(_appSetting["SmtpDisableSsl"]), Credentials = new NetworkCredential(userName, password), Timeout = SmtpTimeoutMilliseconds };
            }
        }

        public string SendInvitationEmail(string email, string code)
        {
            if (Convert.ToBoolean(_appSetting["DoNotSendMail"]))
                email = _appSetting["testMail"];
            if (email == null)
                return SuccessResult;
            var message = new MailMessage(_appSetting["emailFrom"], email)
            {
                IsBodyHtml = true,
                Subject = $"Passi code {code}",
                Body = "<html><body><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" >" +
                "<tr><td>Enter this code in application.</td></tr>" +
                $"<tr><td><b>{code}</b></td></tr>" +
                "</table></body></html>"
            };
            return TrySendWithRetry(message);
        }

        private string TrySendWithRetry(MailMessage message)
        {
            for (int i = 0; i < MaxRetryAttempts; i++)
            {
                try
                {
                    client.Send(message);
                    return SuccessResult;
                }
                catch (Exception e)
                {
                    _logger.Error($"SMTP send attempt {i + 1}/{MaxRetryAttempts} failed: {e.Message}", e);
                    if (i < MaxRetryAttempts - 1)
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
            var message = new MailMessage(_appSetting["smtpUsername"], email)
            {
                IsBodyHtml = true,
                Subject = $"Passi code {code}",
                Body = "<html><body><table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" >" +
                "<tr><td>Enter this code in application.</td></tr>" +
                $"<tr><td><b>{code}</b></td></tr>" +
                "</table></body></html>"
            };
            return TrySendWithRetry(message);
        }
    }
}