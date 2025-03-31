using System;
using System.Threading;
using System.Threading.Tasks;
using ConfigurationManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;


public class Worker : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private AppSetting _appSetting;
    public Worker(IServiceProvider serviceProvider, AppSetting appSetting)
    {
        _serviceProvider = serviceProvider;
        _appSetting = appSetting;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var sampleapp = _appSetting["ClientId"];
        var oldClient = await manager.FindByClientIdAsync(sampleapp);
        if (oldClient != null)
        {
            await manager.DeleteAsync(oldClient);
        }
       


        if (await manager.FindByClientIdAsync(sampleapp) == null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = sampleapp,
                ClientSecret = _appSetting["ClientSecret"],
                ConsentType = ConsentTypes.Explicit,
                DisplayName = "SampleApp client application",
                
                RedirectUris =
                {
                    new Uri("https://localhost/callback/login/local"),
                    new Uri("https://host.docker.internal/callback/login/local"),
                    new Uri("https://passi.cloud/callback/login/local"),
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            });
        }
        var mailLu = _appSetting["MailuClientId"];
        var mailuOldClient = await manager.FindByClientIdAsync(mailLu);
        if (mailuOldClient != null)
        {
            await manager.DeleteAsync(mailuOldClient);
        }
        if (await manager.FindByClientIdAsync(mailLu) == null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = mailLu,
                ClientSecret = _appSetting["MailluSecret"],
                ConsentType = ConsentTypes.Explicit,
                DisplayName = "Maillu client application",

                RedirectUris =
                {
                    new Uri("https://localhost/webmail/oauth2/authorize"),
                    new Uri("https://host.docker.internal/webmail/oauth2/authorize"),
                    new Uri("https://passi.cloud/webmail/oauth2/authorize"),
                    new Uri("https://passi.cloud/sso/login"),
                },
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles
                }
            });
        }
    }


    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
