using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
                        context.Database.Migrate();
                    }
                }

                next(app);
            };
        }
    }
}