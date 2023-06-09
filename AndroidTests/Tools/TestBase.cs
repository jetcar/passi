using System;
using System.Net;
using AppCommon;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using passi_android;
using passi_android.utils;
using passi_android.utils.Services;
using passi_android.utils.Services.Certificate;
using RestSharp;
using WebApiDto;
using Xamarin.Forms;

namespace AndroidTests.Tools;

public class TestBase
{

    [SetUp]
    public void Setup()
    {
        App.Services = ConfigureServices();
        App.IsTest = true;
        App.CancelNotifications = () => { Console.WriteLine("cancel notifications"); };
        App.CloseApp = () => { Console.WriteLine("close ap"); };
        App.StartFingerPrintReading = () => { Console.WriteLine("fingerprint reading started"); };
    }

    public static BaseContentPage CurrentView { get; set; }
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISecureRepository, SecureRepository>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddSingleton<ICertConverter, CertConverter>();
        services.AddSingleton<ICertificatesService, CertificatesService>();
        services.AddSingleton<ICertHelper, CertHelper>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<IMySecureStorage, TestMySecureStorage>();
        services.AddSingleton<IRestService, TestRestService>();
        services.AddSingleton<INavigationService, TestNavigationService>();
        services.AddSingleton<IMainThreadService, TestMainThreadService>();
        services.AddSingleton<IFingerPrintService, FingerPrintService>();

        return services.BuildServiceProvider();
    }
    public static INavigationService Navigation
    {
        get { return App.Services.GetService<INavigationService>(); }
    }

    public static ISecureRepository SecureRepository
    {
        get { return App.Services.GetService<ISecureRepository>(); }
    }

    public static void TouchFingerPrintWithGoodResult()
    {
        Console.WriteLine("touch fingerprint");
        App.FingerPrintReadingResult.Invoke(new FingerPrintResult());
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
                Message = errorMessage
            })
        };
    }
    public static RestResponse SuccesfullResponce<T>(T value)
    {
        Console.WriteLine("rest response: " + JsonConvert.SerializeObject(value));
        return new RestResponse()
        { IsSuccessStatusCode = true, StatusCode = HttpStatusCode.OK, ResponseStatus = ResponseStatus.Completed, Content = JsonConvert.SerializeObject(value) };
    }

    static Random random = new Random();
    public void CreateAccount(bool confirmed = true)
    {
        var certificatesService = App.Services.GetService<ICertificatesService>();
        var secureRepository = App.Services.GetService<ISecureRepository>();
        var certHelper = App.Services.GetService<ICertHelper>();
        var providers = secureRepository.LoadProviders();
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