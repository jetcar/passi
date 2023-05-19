using System;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using passi_android;
using passi_android.utils;
using passi_android.utils.Certificate;
using Xamarin.Forms;

namespace AndroidTests.Tools;

public class TestBase
{

    [SetUp]
    public void Setup()
    {
        App.Services = ConfigureServices();
        App.IsTest = true;
    }

    public static Page CurrentPage { get; set; }
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISecureRepository, SecureRepository>();
        services.AddSingleton<ICertConverter, CertConverter>();
        services.AddSingleton<ICertificatesService, CertificatesService>();
        services.AddSingleton<ICertHelper, CertHelper>();
        services.AddSingleton<ISyncService, SyncService>();
        services.AddSingleton<IMySecureStorage, TestMySecureStorage>();
        services.AddSingleton<IRestService, TestRestService>();
        services.AddSingleton<INavigationService, TestNavigationService>();

        return services.BuildServiceProvider();
    }

    Random random = new Random();
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

    public string GetRandomString(int length)
    {
        var result = "";
        for (int i = 0; i < length; i++)
        {
            var c = random.Next('A', 'Z' + 1);
            result += c;
        }
        return result.ToString();
    }


}