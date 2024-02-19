using System;
using System.Net;
using System.Threading;
using AppCommon;
using AppConfig;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MauiViewModels;
using MauiViewModels.StorageModels;
using MauiViewModels.utils.Services;
using MauiViewModels.utils.Services.Certificate;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using WebApiDto;

namespace MauiTest.Tools;

public class TestBase
{
    [SetUp]
    public void Setup()
    {
        CommonApp.Services = ConfigureServices();
        CommonApp.SkipLoadingTimer = true;

        CommonApp.CancelNotifications = () => { Console.WriteLine("cancel notifications"); };
        CommonApp.CloseApp = () => { Console.WriteLine("close ap"); };
        CommonApp.StartFingerPrintReading = () => { Console.WriteLine("fingerprint reading started"); };
        TestNavigationService.navigationsCount = 0;
        TestNavigationService.AlertMessage = "";
        PrepareDockers();
    }

    [TearDown]
    public void TearDown()
    {
        Thread.Sleep(1000);
    }

    private static IContainer _pgContainer;
    private static IContainer _redisContainer;
    private static IContainer _passiWebApi;
    private static IContainer _identityServer;

    private void PrepareDockers()
    {
        var dockerEndpoint = Environment.GetEnvironmentVariable("DOCKER_HOST");

        var pgpassword = "1";
        if (_pgContainer == null)
        {
            var containerBuilder = new ContainerBuilder()
                .WithImage("postgres:15.5")
                .WithPortBinding(5432, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
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

        if (_redisContainer == null)
        {
            var containerBuilder = new ContainerBuilder()
                .WithImage("redis:latest")
                .WithPortBinding(6379, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(6379));
            if (!string.IsNullOrEmpty(dockerEndpoint))
                containerBuilder.WithDockerEndpoint(dockerEndpoint);

            _redisContainer = containerBuilder
                .Build();
            Console.WriteLine("starting redis container");

            _redisContainer.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        var redisport = _redisContainer
            .GetMappedPublicPort(6379).ToString();

        if (_passiWebApi == null)
        {
            var containerBuilder = new ContainerBuilder()
                    .WithImage("jetcar/passiwebapi:latest")
                    .WithPortBinding(5004, true)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5004))
                    .WithEnvironment("DbHost", _pgContainer.IpAddress)
                    .WithEnvironment("DbPort", "5432")
                    .WithEnvironment("DoNotSendMail", "true")
                    .WithEnvironment("DbPassword", pgpassword)
                    .WithEnvironment("redis", _redisContainer.IpAddress)
                    .WithEnvironment("redisPort", redisport)
                    .WithEnvironment("GOOGLE_APPLICATION_CREDENTIALS", "/home/creds/passi-dev.json")
                    .WithBindMount("d:/home/creds/", "/home/creds/")
                ;

            if (!string.IsNullOrEmpty(dockerEndpoint))
                containerBuilder.WithDockerEndpoint(dockerEndpoint);

            _passiWebApi = containerBuilder
                .Build();
            Console.WriteLine("starting passiapi container");

            _passiWebApi.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        var passiapiport = _passiWebApi.GetMappedPublicPort(5004).ToString();
        var passiInternalUrl = $"http://{_passiWebApi.IpAddress}:{5004}/passiapi";
        ConfigSettings.PassiUrl = $"http://{_passiWebApi.Hostname}:{passiapiport}/passiapi";

        if (_identityServer == null)
        {
            var containerBuilder = new ContainerBuilder()
                    .WithImage("jetcar/identityserver:latest")
                    .WithPortBinding(5003, true)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5003))
                    .WithEnvironment("DbHost", _pgContainer.IpAddress)
                    .WithEnvironment("DbPort", "5432")
                    .WithEnvironment("DoNotSendMail", "true")
                    .WithEnvironment("DbPassword", pgpassword)
                    .WithEnvironment("PassiUrl", passiInternalUrl)
                    .WithEnvironment("GOOGLE_APPLICATION_CREDENTIALS", "/home/creds/passi-dev.json")
                    .WithBindMount("d:/home/creds/", "/home/creds/")
                    .WithBindMount("d:/repo/passi/configs/identity", "/myapp/cert")
                ;

            if (!string.IsNullOrEmpty(dockerEndpoint))
                containerBuilder.WithDockerEndpoint(dockerEndpoint);

            _identityServer = containerBuilder
                .Build();
            Console.WriteLine("starting identity container");

            _identityServer.StartAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        var identityport = _identityServer.GetMappedPublicPort(5003).ToString();

        ConfigSettings.IdentityUrl = $"http://{_identityServer.Hostname}:{identityport}/identity";
        //ConfigSettings.PassiUrl = $"http://localhost:5004/passiapi";
        //ConfigSettings.IdentityUrl = $"http://localhost:5003/identity";
        Console.WriteLine("all containers are started");
    }

    public static BaseViewModel CurrentView { get; set; }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISecureRepository, SecureRepository>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddSingleton<ICertificatesService, CertificatesService>();
        services.AddSingleton<ICertHelper, CertHelper>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<IMySecureStorage, TestMySecureStorage>();
        services.AddSingleton<IRestService, RestService>();
        services.AddSingleton<INavigationService, TestNavigationService>();
        services.AddSingleton<IMainThreadService, TestMainThreadService>();
        services.AddSingleton<IFingerPrintService, FingerPrintService>();

        return services.BuildServiceProvider();
    }

    public static INavigationService Navigation
    {
        get { return CommonApp.Services.GetService<INavigationService>(); }
    }

    public static ISecureRepository SecureRepository
    {
        get { return CommonApp.Services.GetService<ISecureRepository>(); }
    }

    public static void TouchFingerPrintWithGoodResult()
    {
        Console.WriteLine("touch fingerprint");
        CommonApp.FingerPrintReadingResult.Invoke(new FingerPrintResult());
    }

    public static RestResponse SuccesfullResponce()
    {
        Console.WriteLine("rest response ok");
        return new RestResponse()
        { IsSuccessStatusCode = true, StatusCode = HttpStatusCode.OK, ResponseStatus = ResponseStatus.Completed };
    }

    public static RestResponse BadResponce(string errorMessage)
    {
        Console.WriteLine("rest response Bad");
        return new RestResponse()
        {
            IsSuccessStatusCode = false,
            StatusCode = HttpStatusCode.BadRequest,
            ResponseStatus = ResponseStatus.Completed,
            Content = JsonConvert.SerializeObject(new ApiResponseDto<string>()
            {
                errors = errorMessage
            })
        };
    }

    public static RestResponse SuccesfullResponce<T>(T value)
    {
        Console.WriteLine("rest response: " + JsonConvert.SerializeObject(value));
        return new RestResponse()
        { IsSuccessStatusCode = true, StatusCode = HttpStatusCode.OK, ResponseStatus = ResponseStatus.Completed, Content = JsonConvert.SerializeObject(value) };
    }

    private static Random random = new Random();

    public void CreateAccount(bool confirmed = true)
    {
        var certificatesService = CommonApp.Services.GetService<ICertificatesService>();
        var secureRepository = CommonApp.Services.GetService<ISecureRepository>();
        var certHelper = CommonApp.Services.GetService<ICertHelper>();
        var providers = secureRepository.LoadProviders().Result;
        var mail = GetRandomString(6) + "@test.test";
        var cert = certificatesService.GenerateCertificate(mail, new MySecureString("1111"));
        var publicCertHelper = certHelper.ConvertToPublicCertificate(cert.Result.Item1);

        secureRepository.AddAccount(new AccountDb()
        {
            Email = mail,
            Guid = Guid.NewGuid(),
            IsConfirmed = confirmed,
            Provider = providers[0],
            ProviderGuid = providers[0].Guid,
            PrivateCertBinary = Convert.ToBase64String(cert.Result.Item3),
            PublicCertBinary = publicCertHelper.BinaryData,
            pinLength = 4,
            Salt = cert.Result.Item2,
            Thumbprint = cert.Result.Item1.Thumbprint,
            ValidFrom = cert.Result.Item1.NotBefore,
            ValidTo = cert.Result.Item1.NotAfter,
        });
    }

    public static string GetRandomString(int length)
    {
        var result = "";
        for (int i = 0; i < length; i++)
        {
            var c = random.Next('A', 'Z' + 1);
            result += c;
        }
        return result.ToString();
    }

    public static RestResponse FailedResponce()
    {
        Console.WriteLine("rest failed response ");
        return new RestResponse()
        { IsSuccessStatusCode = false, StatusCode = HttpStatusCode.NotFound, ResponseStatus = ResponseStatus.Completed, Content = "error" };
    }
}