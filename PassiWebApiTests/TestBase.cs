using ConfigurationManager;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationsService;
using NUnit.Framework;
using passi_webapi;
using passi_webapi.Controllers;
using RestSharp;
using Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;
using ILogger = log4net.ILog;

namespace PassiWebApiTests;

public class TestBase
{
    private IServiceScope _currentScope;
    public IServiceProvider ServiceProvider { get; private set; }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddScoped<SignUpController>();
        services.AddScoped<CertificateController>();
        services.AddScoped<AuthController>();
        services.AddSingleton<ILog>(LogManager.GetLogger(typeof(TestBase)));
        //IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();
        IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("test.appsettings.json").Build();

        var passiApiStartup = new PassiApiStartup(configuration);
        passiApiStartup.ConfigureServices(services);

        services.Remove(new ServiceDescriptor(typeof(IMyRestClient), typeof(MyRestClient)));//remove real requests services
        services.Remove(new ServiceDescriptor(typeof(IEmailSender), typeof(SendgridEmailSender)));//remove real requests services
        services.Remove(new ServiceDescriptor(typeof(IEmailSender), typeof(SmtpEmailSender)));//remove real requests services
        services.Remove(new ServiceDescriptor(typeof(IFireBaseClient), typeof(FireBaseClient)));
        services.Remove(new ServiceDescriptor(typeof(IFirebaseService), typeof(FirebaseService)));

        services.AddSingleton<IMyRestClient, TestRestClient>();
        services.AddScoped<IEmailSender, TestEmailSender>();
        services.AddSingleton<IFireBaseClient, TestFireBaseClient>();
        services.AddScoped<IFirebaseService, FirebaseService>();

        ServiceProvider = services.BuildServiceProvider();
        PrepareDockers();

        // This call ensures that the latest SQL Server Docker image is pulled

        var startupServices = ServiceProvider.GetServices<IStartupFilter>();
        foreach (var startupService in startupServices)
        {
            startupService.Configure(NextAction).Invoke(new ApplicationBuilder(ServiceProvider));
        }
        passiApiStartup.InitializeDatabase(ServiceProvider);
    }

    [SetUp]
    public void SetUp()
    {
        TestEmailSender.Code = null;
    }

    private static IContainer _pgContainer;
    private static IContainer _redisContainer;

    private void PrepareDockers()
    {
        var appSetting = ServiceProvider.GetService<AppSetting>();

        var dockerEndpoint = Environment.GetEnvironmentVariable("DOCKER_HOST");

        var pgpassword = "1";
        if (_pgContainer == null)
        {
            var containerBuilder = new ContainerBuilder()
                .WithImage("postgres:15.5")
                .WithPortBinding(5432, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("database system is ready to accept connections"))
                .WithEnvironment("POSTGRES_PASSWORD", pgpassword);
            if (!string.IsNullOrEmpty(dockerEndpoint))
                containerBuilder.WithDockerEndpoint(dockerEndpoint);
            _pgContainer = containerBuilder
                .Build();
            Console.WriteLine("starting postgres container");
            _pgContainer.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        var pgPort = _pgContainer
            .GetMappedPublicPort(5432).ToString();
        appSetting["DbHost"] = _pgContainer.Hostname;
        appSetting["DbUser"] = "postgres";
        appSetting["DbPassword"] = pgpassword;
        appSetting["DbPort"] = pgPort;
        if (_redisContainer == null)
        {
            var containerBuilder = new ContainerBuilder()
                .WithImage("redis:latest")
                .WithPortBinding(6379, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilExternalTcpPortIsAvailable(6379));
            if (!string.IsNullOrEmpty(dockerEndpoint))
                containerBuilder.WithDockerEndpoint(dockerEndpoint);

            _redisContainer = containerBuilder
                .Build();
            Console.WriteLine("starting redis container");

            _redisContainer.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        var redisport = _redisContainer
            .GetMappedPublicPort(6379).ToString();
        appSetting["redis"] = _redisContainer.Hostname;
        appSetting["redisPort"] = redisport;

        Console.WriteLine("all containers are started");
    }

    [TearDown]
    public void TearDown()
    {
        CurrentScope = null;
        foreach (var serviceScope in AllScopes)
        {
            serviceScope.Dispose();
        }
    }

    [OneTimeTearDown]
    public void InstanceOnetimeTearDown()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        ServiceProvider = null;
    }

    [OneTimeTearDown]
    public static void OnetimeTearDown()
    {
        if (_pgContainer != null)
        {
            _pgContainer.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _pgContainer = null;
        }
        if (_redisContainer != null)
        {
            _redisContainer.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _redisContainer = null;
        }
    }

    public IServiceScope CurrentScope
    {
        get
        {
            if (_currentScope == null)
            {
                _currentScope = ServiceProvider.CreateScope();
                AllScopes.Add(_currentScope);
            }

            return _currentScope;
        }
        private set => _currentScope = value;
    }

    public List<IServiceScope> AllScopes { get; set; } = new List<IServiceScope>();

    private void NextAction(IApplicationBuilder obj)
    {
    }
}

public class TestEmailSender : IEmailSender
{
    public static string Code;

    public string SendInvitationEmail(String email, String code)
    {
        return Code = code;
    }

    public string SendDeletingEmail(String email, String code)
    {
        return Code = code;
    }
}

public class TestRestClient : IMyRestClient
{
    public Task<RestResponse> ExecuteAsync(RestRequest request)
    {
        throw new NotImplementedException();
    }
}

public class TestFireBaseClient : IFireBaseClient
{
    public string Send(FirebaseAdmin.Messaging.Message message)
    {
        return "test-notification";
    }
}