using Android.App;
using Android.Content.PM;
using AppCommon;
using MauiApp2.FingerPrint;
using MauiApp2.utils.Services;
using MauiApp2.utils.Services.Certificate;

namespace MauiApp2
{

     [Activity(Label = "Passi", Icon = "@mipmap/appicon", Theme = "@style/Maui.SplashTheme", MainLauncher = true, DirectBootAware = true, Exported = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
    public class MainActivity : MauiAppCompatActivity
    {

         internal static readonly string CHANNEL_ID = "passi_notification_channel_id";

        public static App App;
        public static MainActivity Instance;

        private static IServiceProvider ConfigureServices()
        {

            var services = new ServiceCollection();

            services.AddSingleton<ISecureRepository, SecureRepository>();
            services.AddSingleton<IDateTimeService, DateTimeService>();
            services.AddSingleton<ICertificatesService, CertificatesService>();
            services.AddSingleton<ICertHelper, CertHelper>();
            services.AddSingleton<ISyncService, SyncService>();
            services.AddSingleton<IMySecureStorage, MySecureStorage>();
            services.AddSingleton<IRestService, RestService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IMainThreadService, MainThreadService>();
            services.AddSingleton<IFingerPrintService, FingerPrintService>();

            return services.BuildServiceProvider();
        }
        

    }
}