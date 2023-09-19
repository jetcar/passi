using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using ConfigurationManager;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NUnit.Framework;
using passi_webapi;
using passi_webapi.Controllers;
using Repos;
using RestSharp;
using Services;
using ILogger = Serilog.ILogger;
using Logger = Serilog.Core.Logger;
using Message = FirebaseAdmin.Messaging.Message;

namespace PassiWebApiTests;

public class TestBase
{
    private IServiceScope _currentScope;
    public IServiceProvider ServiceProvider { get; private set; }
    static string DB_CONTAINER_NAME = "IntegrationTestingContainer_Accessioning";


    [OneTimeSetUp]
    public void OneTimeSetUp()
    {

        IServiceCollection services = new ServiceCollection();
        services.AddScoped<SignUpController>();
        services.AddSingleton<ILogger>(Logger.None);
        //IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();
        IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("test.appsettings.json").Build();

        var passiApiStartup = new PassiApiStartup(configuration);
        passiApiStartup.ConfigureServices(services);

        services.Remove(new ServiceDescriptor(typeof(IMyRestClient), typeof(MyRestClient)));//remove real requests services
        services.Remove(new ServiceDescriptor(typeof(IEmailSender), typeof(EmailSender)));//remove real requests services
        services.Remove(new ServiceDescriptor(typeof(IFireBaseClient), typeof(FireBaseClient)));//remove real requests services

        services.AddScoped<IMyRestClient, TestRestClient>();
        services.AddScoped<IEmailSender, TestEmailSender>();
        services.AddScoped<IFireBaseClient, TestFireBaseClient>();

        ServiceProvider = services.BuildServiceProvider();
        var appSetting = ServiceProvider.GetService<AppSetting>();
        CleanupRunningContainers().GetAwaiter().GetResult();
        var dockerClient = GetDockerClient();

        string DB_PASSWORD = appSetting["DbPassword"];
        string DB_USER = appSetting["DbUser"];
        string DB_NAME = appSetting["DbName"];
        string DB_IMAGE = "postgres:15.3";
        // This call ensures that the latest SQL Server Docker image is pulled
        StartPostgresContainer(dockerClient, DB_IMAGE, DB_USER, DB_PASSWORD, DB_NAME);

        var startupServices = ServiceProvider.GetServices<IStartupFilter>();
        foreach (var startupService in startupServices)
        {
            startupService.Configure(NextAction).Invoke(new ApplicationBuilder(ServiceProvider));
        }
        passiApiStartup.InitializeDatabase(ServiceProvider);

    }

    private void StartPostgresContainer(DockerClient dockerClient, string dbImage, string dbUser, string dbPassword, string dbName)
    {
        dockerClient.Images.CreateImageAsync(new ImagesCreateParameters
        {
            FromImage = dbImage
        }, null, new Progress<JSONMessage>()).GetAwaiter().GetResult();


        var contList = dockerClient.Containers.ListContainersAsync(new ContainersListParameters() { All = true }).Result;
        var existingCont = contList.FirstOrDefault(c => c.Names.Any(n => n.Contains(DB_CONTAINER_NAME)));

        if (existingCont == null)
        {
            var sqlContainer = dockerClient
                .Containers
                .CreateContainerAsync(new CreateContainerParameters
                {
                    Name = DB_CONTAINER_NAME,
                    Image = dbImage,
                    Env = new List<string>
                    {
                        $"POSTGRES_USER={dbUser}",
                        $"POSTGRES_PASSWORD={dbPassword}",
                        $"POSTGRES_DB={dbName}",
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                "5432/tcp",
                                new PortBinding[]
                                {
                                    new PortBinding
                                    {
                                        HostPort = "5432"
                                    }
                                }
                            }
                        }
                    },
                }).Result;

            dockerClient.Containers.StartContainerAsync(sqlContainer.ID, new ContainerStartParameters()).GetAwaiter()
                .GetResult();

            WaitUntilDatabaseAvailableAsync().GetAwaiter().GetResult();
        }
    }

    [SetUp]
    public void SetUp()
    {
        TestEmailSender.Code = null;
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


    private static DockerClient GetDockerClient()
    {
        return new DockerClientConfiguration().CreateClient();
    }

    private static async Task CleanupRunningContainers(int hoursTillExpiration = -24)
    {
        var dockerClient = GetDockerClient();

        var runningContainers = await dockerClient.Containers
            .ListContainersAsync(new ContainersListParameters());

        foreach (var runningContainer in runningContainers.Where(cont => cont.Names.Any(n => n.Contains(DB_CONTAINER_NAME))))
        {
            // Stopping all test containers that are older than 24 hours
            try
            {
                await EnsureDockerContainersStoppedAndRemovedAsync(runningContainer.ID);
            }
            catch
            {
                // Ignoring failures to stop running containers
            }
        }
    }

    public static async Task EnsureDockerContainersStoppedAndRemovedAsync(string dockerContainerId)
    {
        var dockerClient = GetDockerClient();
        await dockerClient.Containers
            .StopContainerAsync(dockerContainerId, new ContainerStopParameters());
        await dockerClient.Containers
            .RemoveContainerAsync(dockerContainerId, new ContainerRemoveParameters());
    }

    private async Task WaitUntilDatabaseAvailableAsync()
    {
        var start = DateTime.UtcNow;
        const int maxWaitTimeSeconds = 60;
        var connectionEstablished = false;
        using (var scope = ServiceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetService<PassiDbContext>();

            while (!connectionEstablished && start.AddSeconds(maxWaitTimeSeconds) > DateTime.UtcNow)
            {
                try
                {
                    var connection = dbContext.Database.GetDbConnection();
                    connection.Open();
                    connectionEstablished = connection.State == ConnectionState.Open;
                }
                catch
                {
                    // If opening the SQL connection fails, SQL Server is not ready yet
                    await Task.Delay(500);
                }
            }
        }

        if (!connectionEstablished)
        {
            throw new Exception($"Connection to the SQL docker database could not be established within {maxWaitTimeSeconds} seconds.");
        }
    }



}

public class TestFireBaseClient : IFireBaseClient
{
    public string Send(Message message)
    {
        throw new NotImplementedException();
    }
}

public class TestEmailSender : IEmailSender
{
    public static string Code;
    public string SendInvitationEmail(string email, string code)
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