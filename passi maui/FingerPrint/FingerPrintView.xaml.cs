using passi_maui.utils;

namespace passi_maui.FingerPrint
{
    public partial class FingerPrintView : ContentPage
    {
        private readonly AccountDb _accountDb;
        private string _message = "Reading FingerPrint";

        public FingerPrintView(AccountDb accountDb)
        {
            _accountDb = accountDb;
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
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Navigation.PushModalSinglePage(new FingerPrintConfirmByPinView(_accountDb));
                            });
                        else
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                FingerPrintConfirmByPinView.SignRequestAndSendResponce(_accountDb, null, Navigation,
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

            App.CancelfingerPrint();
            Navigation.PopModal();
            element.IsEnabled = true;
        }
    }
}