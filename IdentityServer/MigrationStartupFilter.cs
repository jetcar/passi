using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace IdentityServer
{
    public class MigrationStartupFilter<TContext> : IStartupFilter
        where TContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    foreach (var context in scope.ServiceProvider.GetServices<TContext>())
                    {
                        while (context.Database.GetDbConnection() == null)
                            Thread.Sleep(100);

                        context.Database.Migrate();
                    }
                }

                next(app);
            };
        }
    }
}