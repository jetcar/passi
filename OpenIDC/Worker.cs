using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenIDC.Models;
using OpenIDC.Services;


public class Worker : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<Worker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var clientStore = scope.ServiceProvider.GetRequiredService<IClientStore>();

        var configuredClients = _configuration
            .GetSection("AppSetting:OidcClients")
            .Get<List<OidcClientSeed>>()
            ?? new List<OidcClientSeed>();

        foreach (var configuredClient in configuredClients.Where(c => !string.IsNullOrWhiteSpace(c.ClientId)))
        {
            var resolvedClientSecret = ResolveClientSecret(configuredClient);

            var client = new OidcClient
            {
                ClientId = configuredClient.ClientId,
                ClientSecret = resolvedClientSecret,
                DisplayName = configuredClient.DisplayName,
                RedirectUris = configuredClient.RedirectUris ?? new List<string>(),
                AllowedScopes = configuredClient.AllowedScopes ?? new List<string>(),
                GrantTypes = configuredClient.GrantTypes ?? new List<string>(),
                RequiresPkce = configuredClient.RequiresPkce,
                ConsentType = configuredClient.ConsentType,
                CreatedAt = DateTime.UtcNow
            };

            await clientStore.CreateClientAsync(client);
        }
    }

    private string ResolveClientSecret(OidcClientSeed configuredClient)
    {
        if (!string.IsNullOrWhiteSpace(configuredClient.ClientSecretEnvVar))
        {
            var envValue = Environment.GetEnvironmentVariable(configuredClient.ClientSecretEnvVar)
                           ?? _configuration[configuredClient.ClientSecretEnvVar];

            if (string.IsNullOrWhiteSpace(envValue))
            {
                _logger.LogWarning(
                    "Client secret env var '{SecretEnvVar}' is not set for client '{ClientId}'. Falling back to ClientSecret in config.",
                    configuredClient.ClientSecretEnvVar,
                    configuredClient.ClientId);
            }
            else
            {
                return envValue;
            }
        }

        return configuredClient.ClientSecret;
    }


    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public class OidcClientSeed
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string ClientSecretEnvVar { get; set; }
    public string DisplayName { get; set; }
    public List<string> RedirectUris { get; set; }
    public List<string> AllowedScopes { get; set; }
    public List<string> GrantTypes { get; set; }
    public bool RequiresPkce { get; set; }
    public string ConsentType { get; set; }
}
