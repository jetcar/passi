﻿using System;
using AppCommon;
using passi_android.Main;
using Xamarin.Forms;
using Application = Xamarin.Forms.Application;

namespace passi_android
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new MainView());
            FirstPage = MainPage;
        }

        public static Action StartFingerPrintReading { get; set; }
        public static Action<FingerPrintResult> FingerPrintReadingResult { get; set; }
        public static Action CancelNotifications { get; set; }
        public static Action CloseApp { get; set; }
        public static Action AccountSyncCallback { get; set; }
        public static string Version { get; set; }
        public static IServiceProvider Services { get; set; }
        public static bool IsTest { get; set; }
        public static Page FirstPage { get; set; }
        public static bool SkipLoadingTimer { get; set; }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }

       
    }
}