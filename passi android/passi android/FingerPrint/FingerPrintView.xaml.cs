using System;
using AppCommon;
using passi_android.utils;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.FingerPrint
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FingerPrintView : ContentPage
    {
        private readonly AccountDb _accountDb;
        private string _message = "Reading FingerPrint";

        public FingerPrintView(AccountDb accountDb)
        {
            _accountDb = accountDb;
            InitializeComponent();
        }

        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }


        protected override void OnAppearing()
        {
            if (!App.FingerprintManager.IsHardwareDetected)
            {
                Message = "FingerPrint scanner not found";
                return;
            }
            if (!App.FingerprintManager.HasEnrolledFingerprints)
            {
                Message = "FingerPrints not found";
                return;
            }

            if (!App.IsKeyguardSecure)
            {
                Message = "Secure lock screen hasn\'t been set up. Goto Settings &gt; Security to set up a keyguard.";
                return;
            }

            Message = "";
            App.StartFingerPrintReading();
            App.FingerPrintReadingResult = (result) =>
            {
                if (result.ErrorMessage == null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Navigation.PushModalSinglePage(new FingerPrintConfirmByPinView(_accountDb));
                    });
                }
                else
                {
                    Message = result.ErrorMessage;
                    App.StartFingerPrintReading();
                }
            };
            base.OnAppearing();
        }

        private void CancelButton_OnClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            App.CancelFingerprintReading();
            Navigation.PopModal();
            element.IsEnabled = true;
        }
    }
}