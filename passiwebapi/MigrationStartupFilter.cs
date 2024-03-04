using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace passi_webapi
{
    public class MigrationStartupFilter<TContext> : IStartupFilter where TContext : DbContext
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    foreach (var context in scope.ServiceProvider.GetServices<TContext>())
                    {
                        while (true)
                        {
                            try
                            {
                                while (context.Database.GetDbConnection() == null)
                                    Thread.Sleep(100);
                                break;
                            }
                            catch (Exception e)
                            {
                                Thread.Sleep(100);
                            }
                        }

                        context.Database.SetCommandTimeout(1000);
                        context.Database.Migrate();
                    }
                }

                next(app);
            };
        }
    }
}