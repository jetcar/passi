using System;
using AppCommon;

namespace MauiViewModels
{
    public class App
    {
        public App()
        {
        }

        public static IServiceProvider Services { get; set; }
        public static bool IsTest { get; set; }
        public static Action<FingerPrintResult> FingerPrintReadingResult { get; set; }
        public static Action StartFingerPrintReading { get; set; }
        public static bool SkipLoadingTimer { get; set; }
        public static Action AccountSyncCallback { get; set; }
        public static Action CloseApp { get; set; }
        public static Action CancelNotifications { get; set; }
        public static string Version { get; set; }
    }
}