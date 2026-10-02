using System.Collections.Generic;
using ConfigurationManager;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Services;

namespace ServicesTests;

public class SmtpEmailSenderTests
{
    // DoNotSendMail=true + testMail makes SendDeletingEmailAsync substitute a known "to" address
    // instead of returning early, without actually depending on a reachable SMTP server; smtpHost
    // is a real (but non-listening) endpoint so the SmtpClient is still constructed, matching how
    // production configures it.
    private static AppSetting CreateAppSetting()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
        var appSetting = new AppSetting(configuration);
        appSetting["DoNotSendMail"] = "true";
        appSetting["testMail"] = "someone@example.com";
        appSetting["smtpHost"] = "127.0.0.1";
        appSetting["smtpPort"] = "19999";
        appSetting["smtpPassword"] = "x";
        appSetting["SmtpDisableSsl"] = "true";
        appSetting["emailFrom"] = "admin@passi.cloud";
        // mirrors production's appsettings.json placeholder: not a valid email address
        appSetting["smtpUsername"] = "-";
        return appSetting;
    }

    [Test]
    public void SendDeletingEmailUsesEmailFromNotSmtpUsernameAsTheFromAddress()
    {
        var sender = new SmtpEmailSender(CreateAppSetting());

        Assert.DoesNotThrowAsync(() => sender.SendDeletingEmailAsync("ignored@example.com", "123456"),
            "SendDeletingEmailAsync builds its MailMessage's From address from 'smtpUsername', which in " +
            "production is not a valid email address ('-'); it should use 'emailFrom' like " +
            "SendInvitationEmailAsync does");
    }
}
