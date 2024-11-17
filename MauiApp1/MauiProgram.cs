using AppCommon;
using ChatViewModel;
using IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;

namespace MauiApp1
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // setup OidcClient
            builder.Services.AddSingleton(new OidcClient(new()
            {
                Authority = "https://passi.cloud/identity",

                ClientId = "interactive.public",
                Scope = "openid email",
                RedirectUri = "myapp://callback",
                ClientSecret = "secret3",
                Browser = new MauiAuthenticationBrowser()
            }));
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<MainView>();

            return builder.Build();
        }
    }
}
