using System;
using passi_android.utils;
using passi_android.utils.Services;
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
        INavigationService _navigationService;
        private IMainThreadService _mainThreadService;
        public FingerPrintView(AccountDb accountDb)
        {
            _navigationService = App.Services.GetService<INavigationService>();
            _mainThreadService = App.Services.GetService<IMainThreadService>();
            _accountDb = accountDb;
            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;
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
            if (!App.FingerprintManager.IsHardwareDetected())
            {
                Message = "FingerPrint scanner not found";
            }
            else if (!App.FingerprintManager.HasEnrolledFingerprints())
            {
                Message = "FingerPrints not found.";
            }
            else if (!App.IsKeyguardSecure())
            {
                Message =
                    "Secure lock screen hasn\'t been set up. Goto Settings &gt; Security to set up a keyguard.";
            }
            else
            {
                //Message = "";

                App.StartFingerPrintReading();
                App.FingerPrintReadingResult = (result) =>
                {
                    if (result.ErrorMessage == null)
                    {
                        if (_accountDb.pinLength > 0)
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                _navigationService.PushModalSinglePage(new FingerPrintConfirmByPinView(_accountDb));
                            });
                        else
                            _mainThreadService.BeginInvokeOnMainThread(() =>
                            {
                                FingerPrintConfirmByPinView.SignRequestAndSendResponce(_accountDb, null,
                                    (error) =>
                                    {
                                        Message = error;
                                    });
                            });
                    }
                    else
                    {
                        Message = result.ErrorMessage;
                        App.StartFingerPrintReading();
                    }
                };
            }

            base.OnAppearing();
        }

        private void CancelButton_OnClicked(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            App.CancelFingerprintReading();
            _navigationService.PopModal();
            element.IsEnabled = true;
        }
    }
}