using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

    private static AppSetting CreateIsolatedAppSetting(int smtpPort, int timeoutMilliseconds = 200)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["AppSetting:DoNotSendMail"] = "false",
            ["AppSetting:smtpHost"] = "127.0.0.1",
            ["AppSetting:smtpPort"] = smtpPort.ToString(),
            ["AppSetting:smtpPassword"] = "x",
            ["AppSetting:SmtpDisableSsl"] = "true",
            ["AppSetting:emailFrom"] = "admin@passi.cloud",
            ["AppSetting:smtpUsername"] = "-",
            ["AppSetting:smtpTimeoutMilliseconds"] = timeoutMilliseconds.ToString(),
            ["AppSetting:smtpRetryDelayMilliseconds"] = "0"
        }).Build();

        return new AppSetting(configuration) { PrefferAppsettingFile = true };
    }

    [Test]
    public async Task SendFailureIsPropagatedAndEmailFromIsUsed()
    {
        var sender = new SmtpEmailSender(CreateIsolatedAppSetting(GetUnusedPort()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendDeletingEmailAsync("someone@example.com", "123456"));

        Assert.That(exception.InnerException, Is.Not.TypeOf<FormatException>());
    }

    [Test]
    public void SenderDoesNotShareSmtpClientBetweenConcurrentSends()
    {
        var smtpClientFields = typeof(SmtpEmailSender)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Where(field => typeof(SmtpClient).IsAssignableFrom(field.FieldType));

        Assert.That(smtpClientFields, Is.Empty);
    }

    [Test]
    public async Task AsyncSendHonorsConfiguredTimeout()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var stopServer = new CancellationTokenSource();
        var acceptedClients = new List<TcpClient>();
        var serverTask = AcceptConnectionsAsync(listener, acceptedClients, stopServer.Token);
        var sender = new SmtpEmailSender(CreateIsolatedAppSetting(port, 100));
        var stopwatch = Stopwatch.StartNew();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.SendInvitationEmailAsync("someone@example.com", "123456"));

        stopwatch.Stop();
        stopServer.Cancel();
        listener.Stop();
        await serverTask;
        foreach (var client in acceptedClients)
            client.Dispose();

        Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
    }

    // DoNotSendMail=true with testMail set but smtpHost left as "-" (the appsettings.json
    // placeholder for "no real SMTP server configured") leaves the SmtpClient null in the
    // constructor; the email is still resolved to testMail, so the Send*Async methods must not
    // call into the null client.
    [Test]
    public async Task SendInvitationEmailReturnsOkWhenDoNotSendMailIsTrueAndSmtpHostIsNotConfigured()
    {
        var appSetting = CreateAppSetting();
        appSetting["smtpHost"] = "-";

        var sender = new SmtpEmailSender(appSetting);

        var result = await sender.SendInvitationEmailAsync("ignored@example.com", "123456");

        Assert.That(result, Is.EqualTo("ok"));
    }

    [Test]
    public async Task SendDeletingEmailReturnsOkWhenDoNotSendMailIsTrueAndSmtpHostIsNotConfigured()
    {
        var appSetting = CreateAppSetting();
        appSetting["smtpHost"] = "-";

        var sender = new SmtpEmailSender(appSetting);

        var result = await sender.SendDeletingEmailAsync("ignored@example.com", "123456");

        Assert.That(result, Is.EqualTo("ok"));
    }

    [Test]
    public void MessageContainsTheSharedLocalTemplateWithPlainTextFallback()
    {
        using var message = SmtpEmailSender.CreateMessage(
            "admin@passi.cloud",
            "someone@example.com",
            "123456");
        var plainTextView = message.AlternateViews.Single(view => view.ContentType.MediaType == "text/plain");
        using var reader = new StreamReader(plainTextView.ContentStream, Encoding.UTF8, leaveOpen: true);
        var plainText = reader.ReadToEnd();

        Assert.Multiple(() =>
        {
            Assert.That(message.Subject, Is.EqualTo("Passi confirmation code: 123456"));
            Assert.That(message.Body, Does.Contain(">123456</p>"));
            Assert.That(plainText, Does.Contain("confirmation code is: 123456"));
        });
    }

    private static int GetUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task AcceptConnectionsAsync(
        TcpListener listener,
        ICollection<TcpClient> acceptedClients,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
                acceptedClients.Add(await listener.AcceptTcpClientAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
        }
        catch (SocketException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
