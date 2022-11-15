using System;
using AppCommon;
using Xamarin.Forms;
using Application = Xamarin.Forms.Application;

namespace passi_android
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new MainPage());
            FingerprintManager = new FingerPrintWrapper();
        }

        public static FingerPrintWrapper FingerprintManager { get; set; }
        public static Action StartFingerPrintReading { get; set; }
        public static Action CancelfingerPrint { get; set; }
        public static Action<FingerPrintResult> FingerPrintReadingResult { get; set; }
        public static Action CancelNotifications { get; set; }
        public static Action PollNotifications { get; set; }
        public static Action CloseApp { get; set; }
        public static bool IsKeyguardSecure { get; set; }
        public static Action AccountSyncCallback { get; set; }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }

        public static void CancelFingerprintReading()
        {
            if (CancelfingerPrint != null) CancelfingerPrint.Invoke();
        }
    }

    public class FingerPrintWrapper
    {
        public bool HasEnrolledFingerprints { get; set; }
        public bool IsHardwareDetected { get; set; }
    }
}