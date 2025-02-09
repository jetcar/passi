using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using ConfigurationManager;
using Serilog;


public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{

    private readonly AppSetting _appSetting;
    private ILogger _logger;


    //public ApplicationDbContext()
    //{
    //    var myConfiguration = new Dictionary<string, string>
    //    {
    //        {"AppSetting:IdentityDbName", "Identity"},
    //        {"AppSetting:DbUser", "postgres"},
    //        {"AppSetting:DbPassword", "q"},
    //        {"AppSetting:DbHost", "localhost"},
    //    };
    //    var config = new ConfigurationBuilder().AddInMemoryCollection(myConfiguration).Build();
    //    var appSetting = new AppSetting(config);
    //    appSetting.PrefferAppsettingFile = true;
    //    _logger = Logger.None;
    //    _appSetting = appSetting;
    //}
    public ApplicationDbContext(AppSetting appSetting, ILogger logger)

    {
        _appSetting = appSetting;
        _logger = logger;
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // base.OnConfiguring(optionsBuilder);
        _logger.Debug("dbhost=" + _appSetting["DbHost"]);
        var trustMode = _appSetting["DbSslMode"] == "Require" ? "Trust Server Certificate=true;" : "";
        //optionsBuilder.AddInterceptors(new TaggedQueryCommandInterceptor(_logger));
        var connectionString = $"host={_appSetting["DbHost"]};port={_appSetting["DbPort"]};database={_appSetting["IdentityDbName"]};user id={_appSetting["DbUser"]};password={_appSetting["DbPassword"]};Ssl Mode={_appSetting["DbSslMode"]};{trustMode}";
        Console.WriteLine(connectionString);

        optionsBuilder.UseOpenIddict();
        optionsBuilder.UseNpgsql(connectionString, o =>
        {
            o.EnableRetryOnFailure(30, TimeSpan.FromSeconds(2), null);
        });

    }
}
