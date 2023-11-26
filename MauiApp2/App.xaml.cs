using AppCommon;

namespace MauiApp2
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        public static IServiceProvider Services { get; set; }
        public static bool IsTest { get; set; }
        public static Page FirstPage { get; set; }
        public static Action<FingerPrintResult> FingerPrintReadingResult { get; set; }
        public static Action StartFingerPrintReading { get; set; }
        public static bool SkipLoadingTimer { get; set; }
        public static Action AccountSyncCallback { get; set; }
        public static Action CloseApp { get; set; }
        public static Action CancelNotifications { get; set; }
        public static string Version { get; set; }

    }
}