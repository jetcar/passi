using passi_android.utils;
using passi_android.utils.Services.Certificate;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Color = Xamarin.Forms.Color;

namespace passi_android.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UpdateCertificateView : BaseContentPage
    {
        private string _pin1Masked;
        private string _pin2Masked;
        private string _pinOldMasked;
        private MySecureString _pin1 = new MySecureString("");
        private MySecureString _pin2 = new MySecureString("");
        private MySecureString _pinOld = new MySecureString("");
        private Color _pin1Color;
        private Color _pin2Color;
        private Color _pinOldColor;
        private string _responseError;
        private ValidationError _pin1Error = new ValidationError();
        private ValidationError _pin2Error = new ValidationError();
        private ValidationError _pinOldError = new ValidationError();

        private readonly int MinPinLenght = 4;

        public UpdateCertificateView()
        {
            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;
            SetTextBox(0);
        }

        public string Pin1Masked
        {
            get => _pin1Masked;
            set
            {
                _pin1Masked = value;
                if (Pin1.Length > MinPinLenght)
                {
                    Pin1Error.HasError = false;
                    Pin1Error.Text = "";
                }
                OnPropertyChanged();
            }
        }

        public string PinOldMasked
        {
            get => _pinOldMasked;
            set
            {
                _pinOldMasked = value;
                if (PinOld.Length > MinPinLenght)
                {
                    PinOldError.HasError = false;
                    PinOldError.Text = "";
                }
                OnPropertyChanged();
            }
        }

        public string Pin2Masked
        {
            get => _pin2Masked;
            set
            {
                _pin2Masked = value;

                if (Pin1.Equals(Pin2))
                {
                    Pin2Error.HasError = false;
                    Pin2Error.Text = "";
                }
                OnPropertyChanged();
            }
        }

        public MySecureString Pin1
        {
            get => _pin1;
            set
            {
                _pin1 = value;
                Pin1Masked = _pin1.GetMasked("*");
            }
        }

        public MySecureString PinOld
        {
            get => _pinOld;
            set
            {
                _pinOld = value;
                PinOldMasked = _pinOld.GetMasked("*");
            }
        }

        public MySecureString Pin2
        {
            get => _pin2;
            set
            {
                _pin2 = value;
                Pin2Masked = _pin2.GetMasked("*");
            }
        }

        private int currentfieldIndex = 0;

        public void SetTextBox(int i)
        {
            currentfieldIndex = i;
            if (i == 0)
            {
                PinOldColor = Color.Aquamarine;
                Pin1Color = this.BackgroundColor;
                Pin2Color = this.BackgroundColor;
            }
            if (i == 1)
            {
                Pin1Color = Color.Aquamarine;
                Pin2Color = this.BackgroundColor;
                PinOldColor = this.BackgroundColor;
            }
            if (i == 2)
            {
                Pin2Color = Color.Aquamarine;
                Pin1Color = this.BackgroundColor;
                PinOldColor = this.BackgroundColor;
            }
        }

        public Color Pin1Color
        {
            get => _pin1Color;
            set
            {
                _pin1Color = value;
                OnPropertyChanged();
            }
        }

        public Color PinOldColor
        {
            get => _pinOldColor;
            set
            {
                _pinOldColor = value;
                OnPropertyChanged();
            }
        }

        public Color Pin2Color
        {
            get => _pin2Color;
            set
            {
                _pin2Color = value;
                OnPropertyChanged();
            }
        }

        public string ResponseError
        {
            get => _responseError;
            set
            {
                _responseError = value;
                OnPropertyChanged();
            }
        }

        public ValidationError Pin1Error
        {
            get => _pin1Error;
            set => _pin1Error = value;
        }

        public ValidationError Pin2Error
        {
            get => _pin2Error;
            set => _pin2Error = value;
        }

        public ValidationError PinOldError
        {
            get => _pinOldError;
            set => _pinOldError = value;
        }

        public AccountDb Account { get; set; }

        public void NumbersPad_OnNumberClicked(string value)
        {
            if (value == "confirm")
            {
                if (currentfieldIndex == 2)
                {
                    ResponseError = "";
                    PinOldError.Clear();
                    Pin1Error.Clear();
                    Pin2Error.Clear();

                    if (Pin1.Length < MinPinLenght)
                    {
                        SetTextBox(1);
                        //error
                        Pin1Error.HasError = true;
                        Pin1Error.Text = $"Pin1 should be min {MinPinLenght} numbers";

                        return;
                    }

                    if (!Pin1.Equals(Pin2))
                    {
                        SetTextBox(2);
                        //error
                        Pin2Error.HasError = true;
                        Pin2Error.Text = "Pin2 doesn't match with Pin1";

                        return;
                    }

                    _certificatesService.UpdateCertificate(Pin1, PinOld, Account, (error) =>
                    {
                        _mainThreadService.BeginInvokeOnMainThread(() =>
                        {
                            _navigationService.PopModal().ContinueWith((task =>
                            {
                                if (error == "Invalid old pin")
                                {
                                    PinOldError.HasError = true;
                                    PinOldError.Text = "Invalid old pin";

                                    SetTextBox(0);
                                }
                                else
                                {
                                    ResponseError = error;
                                }
                            }));
                        });
                    });
                    return;
                }
                if (currentfieldIndex == 0)
                {
                    SetTextBox(1);
                    return;
                }
                if (currentfieldIndex == 1)
                {
                    SetTextBox(2);
                }
                return;
            }
            if (value == "del")
            {
                if (currentfieldIndex == 0)
                    if (PinOld.Length > 0)
                        PinOld = PinOld.TrimEnd(1);
                if (currentfieldIndex == 1)
                    if (Pin1.Length > 0)
                        Pin1 = Pin1.TrimEnd(1);
                if (currentfieldIndex == 2)
                    if (Pin2.Length > 0)
                        Pin2 = Pin2.TrimEnd(1);
                return;
            }

            if (currentfieldIndex == 0)
            {
                PinOld.AppendChar(value);
                PinOldMasked = PinOld.GetMasked("*");
            }

            if (currentfieldIndex == 1)
            {
                Pin1.AppendChar(value);
                Pin1Masked = Pin1.GetMasked("*");
            }

            if (currentfieldIndex == 2)
            {
                Pin2.AppendChar(value);
                Pin2Masked = Pin2.GetMasked("*");
            }
        }

        public void ClearPin1_OnClicked(object sender, EventArgs e)
        {
            Pin1 = new MySecureString("");
            SetTextBox(1);
        }

        public void ClearPin2_OnClicked(object sender, EventArgs e)
        {
            Pin2 = new MySecureString("");
            SetTextBox(2);
        }

        public void ClearPinOld_OnClicked(object sender, EventArgs e)
        {
            PinOld = new MySecureString("");
            SetTextBox(0);
        }

        public void Button_Cancel(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            _navigationService.PopModal();
            element.IsEnabled = true;
        }
    }
}