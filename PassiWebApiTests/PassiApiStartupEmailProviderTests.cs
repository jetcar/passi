using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using passi_webapi;
using Services;

namespace PassiWebApiTests;

[NonParallelizable]
public class PassiApiStartupEmailProviderTests
{
    private string _originalEmailProvider;
    private string _originalIsTest;

    [SetUp]
    public void SetUp()
    {
        _originalEmailProvider = Environment.GetEnvironmentVariable("EmailProvider");
        _originalIsTest = Environment.GetEnvironmentVariable("IsTest");
        Environment.SetEnvironmentVariable("IsTest", "false");
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("EmailProvider", _originalEmailProvider);
        Environment.SetEnvironmentVariable("IsTest", _originalIsTest);
    }

    [TestCase(null, typeof(SendgridEmailSender))]
    [TestCase("SendGrid", typeof(SendgridEmailSender))]
    [TestCase("Smtp", typeof(SmtpEmailSender))]
    public void ConfigureServices_SelectsConfiguredEmailProvider(string emailProvider, Type expectedType)
    {
        Environment.SetEnvironmentVariable("EmailProvider", emailProvider);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AppSetting:projectId"] = "test-project"
            })
            .Build();
        var services = new ServiceCollection();

        new PassiApiStartup(configuration).ConfigureServices(services);

        var descriptor = services.Single(service => service.ServiceType == typeof(IEmailSender));
        Assert.That(descriptor.ImplementationType, Is.EqualTo(expectedType));
    }

    [TestCase("Unknown")]
    [TestCase(" ")]
    public void ConfigureServices_RejectsUnsupportedEmailProvider(string emailProvider)
    {
        Environment.SetEnvironmentVariable("EmailProvider", emailProvider);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AppSetting:projectId"] = "test-project"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            new PassiApiStartup(configuration).ConfigureServices(new ServiceCollection()));
    }

    [Test]
    public void ConfigureServices_RejectsEmptyConfiguredEmailProvider()
    {
        Environment.SetEnvironmentVariable("EmailProvider", null);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AppSetting:projectId"] = "test-project",
                ["AppSetting:EmailProvider"] = ""
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            new PassiApiStartup(configuration).ConfigureServices(new ServiceCollection()));
    }
}
