using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIDC.Models;
using OpenIDC.Services;


public class Worker : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AppSetting _appSetting;

    public Worker(IServiceProvider serviceProvider, AppSetting appSetting)
    {
        _serviceProvider = serviceProvider;
        _appSetting = appSetting;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var clientStore = scope.ServiceProvider.GetRequiredService<IClientStore>();

        // Initialize SampleApp client
        var sampleAppId = _appSetting["ClientId"];
        var sampleAppClient = new OidcClient
        {
            ClientId = sampleAppId,
            ClientSecret = _appSetting["ClientSecret"],
            DisplayName = "SampleApp client application",
            RedirectUris = new List<string>
            {
                "https://localhost/callback/login/local",
                "https://localhost/sampleapi/callback/login/local",
                "https://host.docker.internal/callback/login/local",
                "https://passi.cloud/callback/login/local"
            },
            AllowedScopes = new List<string> { "openid", "profile", "email", "roles" },
            RequiresPkce = true,
            CreatedAt = DateTime.UtcNow
        };
        await clientStore.CreateClientAsync(sampleAppClient);

        // Initialize Mailu client
        var mailuId = _appSetting["MailuClientId"];
        var mailuClient = new OidcClient
        {
            ClientId = mailuId,
            ClientSecret = _appSetting["MailuSecret"],
            DisplayName = "Maillu client application",
            RedirectUris = new List<string>
            {
                "https://localhost/webmail/oauth2/authorize",
                "https://host.docker.internal/webmail/oauth2/authorize",
                "https://passi.cloud/webmail/oauth2/authorize",
                "https://passi.cloud/sso/login"
            },
            AllowedScopes = new List<string> { "openid", "profile", "email", "roles" },
            RequiresPkce = false,
            CreatedAt = DateTime.UtcNow
        };
        await clientStore.CreateClientAsync(mailuClient);

        // Initialize SampleApi client
        var sampleApiId = _appSetting["SampleApiClientId"];
        var sampleApiClient = new OidcClient
        {
            ClientId = sampleApiId,
            ClientSecret = _appSetting["SampleApiSecret"],
            DisplayName = "Sample API client application",
            RedirectUris = new List<string>
            {
                "https://localhost/sampleapi/callback",
                "https://host.docker.internal/sampleapi/callback",
                "https://passi.cloud/sampleapi/callback"
            },
            AllowedScopes = new List<string> { "openid", "profile", "email", "api" },
            RequiresPkce = false,
            CreatedAt = DateTime.UtcNow
        };
        await clientStore.CreateClientAsync(sampleApiClient);
    }


    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
