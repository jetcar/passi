using System;
using System.Net;
using System.Net.Mail;
using System.Net;
using System.Net.Mail;
using ConfigurationManager;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using GoogleTracer;
using Microsoft.Extensions.Logging;
using Message = FirebaseAdmin.Messaging.Message;

namespace NotificationsService
{
    [Profile]
    public class FireBaseClient : IFireBaseClient
    {
        public ILogger<FireBaseClient> _logger;
        private AppSetting _appSetting;

        public FireBaseClient(ILogger<FireBaseClient> logger, AppSetting appSetting)
        {
            _logger = logger;
            _appSetting = appSetting;
            var filePath = _appSetting["google-services-json-path"];
            var credential = GoogleCredential.FromFile(filePath);
            FirebaseApp.Create(new AppOptions()
            {
                Credential = credential
            });
        }

        public string Send(Message message)
        {
            if (string.IsNullOrEmpty(message.Token))
            {
                _logger.LogDebug("token is missing");
                return null;
            }
            return FirebaseMessaging.DefaultInstance.SendAsync(message).Result;
        }
    }

    /// <summary>
    /// Test-mode Firebase replacement: instead of calling Firebase, forwards the notification
    /// payload as an SMTP email to the test mail service (mailhog). This lets E2E tests detect
    /// pending sessions by polling mailhog and trigger the full notification/auth flow.
    /// </summary>
    [Profile]
    public class ExternalServiceCatcherClient : IFireBaseClient
    {
        private const string CatcherRecipient = "notification-catcher@passi.test";

        private readonly ILogger<ExternalServiceCatcherClient> _logger;
        private readonly AppSetting _appSetting;

        public ExternalServiceCatcherClient(ILogger<ExternalServiceCatcherClient> logger, AppSetting appSetting)
        {
            _logger = logger;
            _appSetting = appSetting;
        }

        public string Send(Message message)
        {
            var payload = BuildPayload(message);
            _logger.LogInformation("ExternalServiceCatcherClient: forwarding notification to test mail service. payload={Payload}", payload);

            try
            {
                var host = _appSetting["smtpHost"];
                var port = int.Parse(_appSetting["smtpPort"] ?? "1025");
                var from = _appSetting["emailFrom"] ?? "passi@test.com";
                var disableSsl = bool.Parse(_appSetting["SmtpDisableSsl"] ?? "true");

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = !disableSsl,
                    Credentials = new NetworkCredential(
                        _appSetting["smtpUsername"],
                        _appSetting["smtpPassword"]),
                };

                var mail = new MailMessage(from, CatcherRecipient)
                {
                    Subject = "Passi notification " + (message.Data != null && message.Data.ContainsKey("title") ? message.Data["title"] : "push"),
                    Body = payload,
                    IsBodyHtml = false,
                };

                client.Send(mail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExternalServiceCatcherClient: failed to forward notification to test mail service");
            }

            return "catcher-ok";
        }

        private static string BuildPayload(Message message)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (message.Notification?.Body != null)
                parts.Add(message.Notification.Body);
            if (message.Data != null)
                foreach (var kv in message.Data)
                    parts.Add($"{kv.Key}={kv.Value}");
            return string.Join("\n", parts);
        }
    }

    public interface IFireBaseClient
    {
        string Send(Message message);
    }
}