using System.Collections.Generic;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Services;

namespace ServicesTests;

public class SendgridEmailSenderTests
{
    // DoNotSendMail=true with testMail set but no SendgridApiKey is exactly the local-dev config
    // combo that leaves the SendGrid client null in the constructor; the email is still resolved
    // to testMail, so the Send*Async methods must not call into the null client.
    private static AppSetting CreateAppSetting()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build();
        var appSetting = new AppSetting(configuration);
        appSetting["DoNotSendMail"] = "true";
        appSetting["testMail"] = "someone@example.com";
        appSetting["SendgridApiKey"] = "";
        appSetting["EmailFrom"] = "admin@passi.cloud";
        return appSetting;
    }

    [Test]
    public async Task SendInvitationEmailReturnsOkWhenDoNotSendMailIsTrueAndNoSendgridApiKeyIsConfigured()
    {
        var sender = new SendgridEmailSender(CreateAppSetting());

        var result = await sender.SendInvitationEmailAsync("ignored@example.com", "123456");

        Assert.That(result, Is.EqualTo("ok"));
    }

    [Test]
    public async Task SendDeletingEmailReturnsOkWhenDoNotSendMailIsTrueAndNoSendgridApiKeyIsConfigured()
    {
        var sender = new SendgridEmailSender(CreateAppSetting());

        var result = await sender.SendDeletingEmailAsync("ignored@example.com", "123456");

        Assert.That(result, Is.EqualTo("ok"));
    }
}
