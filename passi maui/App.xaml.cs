using AppCommon;
using Microsoft.Maui.Controls.PlatformConfiguration;
using passi_maui.utils;
using System.Security.Cryptography.X509Certificates;

namespace passi_maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            App.Version = AppInfo.Current.VersionString;

            return base.CreateWindow(activationState);
        }

        public static FingerPrintWrapper FingerprintManager { get; set; }
        public static Action StartFingerPrintReading { get; set; }
        public static Action CancelfingerPrint { get; set; }
        public static Action<FingerPrintResult> FingerPrintReadingResult { get; set; }
        public static Action CancelNotifications { get; set; }
        public static Action PollNotifications { get; set; }
        public static Action CloseApp { get; set; }
        public static Func<bool> IsKeyguardSecure { get; set; }
        public static Action AccountSyncCallback { get; set; }
        public static string Version { get; set; }
        public static Func<Tuple<X509Certificate2, string, byte[]>> CreateCertificate { get; set; }
    }
}