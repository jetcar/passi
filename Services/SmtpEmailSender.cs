using ConfigurationManager;
using System;
using System.Net;
using GoogleTracer;
using System.Net.Mail;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Services
{
    [Profile]
    public class SmtpEmailSender : IEmailSender
    {
        private const int MaxRetryAttempts = 3;
        private const int DefaultRetryDelayMilliseconds = 2000;
        private const int DefaultSmtpTimeoutMilliseconds = 15_000;
        private const string SuccessResult = "ok";

        private readonly AppSetting _appSetting;
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SmtpEmailSender));

        public SmtpEmailSender(AppSetting appSetting)
        {
            _appSetting = appSetting;
        }

        public async Task<string> SendInvitationEmailAsync(string email, string code)
        {
            var doNotSendMail = Convert.ToBoolean(_appSetting["DoNotSendMail"]);
            if (doNotSendMail)
                email = _appSetting["testMail"];
            if (email == null || (doNotSendMail && !IsSmtpConfigured()))
                return SuccessResult;

            return await TrySendWithRetryAsync(email, code);
        }

        private async Task<string> TrySendWithRetryAsync(string email, string code)
        {
            Exception lastException = null;
            var retryDelayMilliseconds = GetNonNegativeIntSetting("smtpRetryDelayMilliseconds", DefaultRetryDelayMilliseconds);
            var smtpTimeoutMilliseconds = GetPositiveIntSetting("smtpTimeoutMilliseconds", DefaultSmtpTimeoutMilliseconds);

            for (int i = 0; i < MaxRetryAttempts; i++)
            {
                try
                {
                    using var message = CreateMessage(_appSetting["emailFrom"], email, code);
                    using var client = CreateClient();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(smtpTimeoutMilliseconds));
                    await client.SendMailAsync(message, timeout.Token);
                    return SuccessResult;
                }
                catch (Exception e)
                {
                    lastException = e;
                    _logger.Error($"SMTP send attempt {i + 1}/{MaxRetryAttempts} failed: {e.Message}", e);
                    if (i < MaxRetryAttempts - 1)
                        await Task.Delay(retryDelayMilliseconds);
                }
            }

            throw new InvalidOperationException($"SMTP send failed after {MaxRetryAttempts} attempts.", lastException);
        }

        public async Task<string> SendDeletingEmailAsync(string email, string code)
        {
            var doNotSendMail = Convert.ToBoolean(_appSetting["DoNotSendMail"]);
            if (doNotSendMail)
                email = _appSetting["testMail"];
            if (email == null || (doNotSendMail && !IsSmtpConfigured()))
                return SuccessResult;

            return await TrySendWithRetryAsync(email, code);
        }

        internal static MailMessage CreateMessage(string from, string to, string code)
        {
            var content = PassiEmailTemplate.CreateConfirmationCode(code);
            var message = new MailMessage(from, to)
            {
                IsBodyHtml = true,
                Subject = content.Subject,
                Body = content.Html
            };
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                content.PlainText,
                null,
                MediaTypeNames.Text.Plain));
            return message;
        }

        private bool IsSmtpConfigured()
        {
            var host = _appSetting["smtpHost"];
            return !string.IsNullOrEmpty(host) && host != "-";
        }

        private SmtpClient CreateClient()
        {
            return new SmtpClient
            {
                Host = _appSetting["smtpHost"],
                Port = Convert.ToInt32(_appSetting["smtpPort"]),
                EnableSsl = !Convert.ToBoolean(_appSetting["SmtpDisableSsl"]),
                Credentials = new NetworkCredential(_appSetting["smtpUsername"], _appSetting["smtpPassword"])
            };
        }

        private int GetPositiveIntSetting(string key, int defaultValue)
        {
            return int.TryParse(_appSetting[key], out var value) && value > 0 ? value : defaultValue;
        }

        private int GetNonNegativeIntSetting(string key, int defaultValue)
        {
            return int.TryParse(_appSetting[key], out var value) && value >= 0 ? value : defaultValue;
        }
    }
}
