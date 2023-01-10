using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AppCommon;
using AppConfig;
using Newtonsoft.Json;
using passi_android.Tools;
using passi_android.utils;
using RestSharp;
using WebApiDto;
using WebApiDto.SignUp;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using CertHelper = passi_android.utils.Certificate.CertHelper;
using Certificates = passi_android.utils.Certificate.Certificates;
using Color = Xamarin.Forms.Color;

namespace passi_android.Registration
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FinishConfirmation : ContentPage
    {
        private string _pin1Masked;
        private string _pin2Masked;
        private string _pin1 = "";
        private string _pin2 = "";
        private Color _pin1Color;
        private Color _pin2Color;
        private bool _secondPin;
        private string _responseError;
        private ValidationError _pin1Error = new ValidationError();
        private ValidationError _pin2Error = new ValidationError();

        private readonly int MinPinLenght = 4;

        public FinishConfirmation()
        {
            InitializeComponent();
            BindingContext = this;
            SecondPin = false;
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

        public string Pin2Masked
        {
            get => _pin2Masked;
            set
            {
                _pin2Masked = value;

                if (Pin1 == Pin2)
                {
                    Pin2Error.HasError = false;
                    Pin2Error.Text = "";
                }
                OnPropertyChanged();
            }
        }

        public string Pin1
        {
            get => _pin1;
            set
            {
                _pin1 = value;
                Pin1Masked = new string(_pin1.ToList().Select(x => '*').ToArray());
            }
        }

        public string Pin2
        {
            get => _pin2;
            set
            {
                _pin2 = value;
                Pin2Masked = new string(_pin2.ToList().Select(x => '*').ToArray());
            }
        }

        protected override void OnAppearing()
        {
            EmailText = Account.Email;
            base.OnAppearing();
        }

        private void Confirm(string code, string email, string pin)
        {
            Navigation.PushModalSinglePage(new LoadingPage(() =>
            {
                GenerateCert(email, pin).ContinueWith(x =>
                {
                    var signupConfirmationDto = new SignupConfirmationDto()
                    {
                        Code = code,
                        PublicCert = x.Result.PublicCertBinary,
                        Email = email,
                        Guid = x.Result.Guid.ToString(),
                        DeviceId = SecureRepository.GetDeviceId()
                    };

                    RestService.ExecutePostAsync(Account.Provider, Account.Provider.SignupConfirmation, signupConfirmationDto).ContinueWith((response) =>
                    {
                        if (response.Result.IsSuccessful)
                        {
                            SecureRepository.UpdateAccount(x.Result);
                            MainThread.BeginInvokeOnMainThread(() => { Navigation.NavigateTop(); });
                        }
                        else if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Navigation.PopModal().ContinueWith((task =>
                                {
                                    ResponseError = JsonConvert
                                        .DeserializeObject<ApiResponseDto<string>>(response.Result.Content).Message;
                                }));
                            });
                        }
                        else
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Navigation.PopModal().ContinueWith((s) =>
                                {
                                    ResponseError = "Network error. Try again";
                                });
                            });
                        }
                    });
                });
            }));
        }

        public string Code { get; set; }
        public string EmailText { get; set; }

        private async Task<AccountDb> GenerateCert(string email, string pin)
        {
            return await Certificates.GenerateCertificate(email, pin).ContinueWith(certificate =>
             {
                 Account.Password = certificate.Result.Item2;
                 var certificateBytes = Convert.ToBase64String(certificate.Result.Item3); //importable certificate
                 Account.PrivateCertBinary = certificateBytes;
                 Account.pinLength = pin?.Length ?? 0;
                 var publicCertHelper = CertHelper.ConvertToPublicCertificate(certificate.Result.Item1);
                 Account.Thumbprint = publicCertHelper.Thumbprint;
                 Account.ValidFrom = publicCertHelper.NotBefore.Value;
                 Account.ValidTo = publicCertHelper.NotAfter.Value;
                 Account.PublicCertBinary = publicCertHelper.BinaryData;
                 return Account;
             });
        }

        public bool SecondPin
        {
            get => _secondPin;
            set
            {
                _secondPin = value;
                if (!value)
                {
                    Pin1Color = Color.Aquamarine;
                    Pin2Color = this.BackgroundColor;
                }
                else
                {
                    Pin2Color = Color.Aquamarine;
                    Pin1Color = this.BackgroundColor;
                }
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

        public AccountDb Account { get; set; }

        private void NumbersPad_OnNumberClicked(string value)
        {
            if (value == "confirm")
            {
                if (SecondPin)
                {
                    ResponseError = "";

                    if (Pin1.Length < MinPinLenght)
                    {
                        SecondPin = false;
                        //error
                        Pin1Error.HasError = true;
                        Pin1Error.Text = $"Pin1 should be min {MinPinLenght} numbers";

                        return;
                    }

                    if (Pin1 != Pin2)
                    {
                        SecondPin = false;
                        //error
                        Pin2Error.HasError = true;
                        Pin2Error.Text = "Pin2 doesn't match with Pin1";

                        return;
                    }

                    Confirm(Code, EmailText, Pin1);
                }
                if (!SecondPin)
                {
                    SecondPin = true;
                }
                return;
            }
            if (value == "del")
            {
                if (!SecondPin)
                    if (Pin1.Length > 0)
                        Pin1 = Pin1.Substring(0, Pin1.Length - 1);
                if (SecondPin)
                    if (Pin2.Length > 0)
                        Pin2 = Pin1.Substring(0, Pin2.Length - 1);
                return;
            }
            if (!SecondPin)
                Pin1 += value;
            if (SecondPin)
                Pin2 += value;
        }

        private void ClearPin1_OnClicked(object sender, EventArgs e)
        {
            Pin1 = "";
            SecondPin = false;
        }

        private void ClearPin2_OnClicked(object sender, EventArgs e)
        {
            Pin2 = "";
            SecondPin = true;
        }

        private void SkipButton_OnClicked(object sender, EventArgs e)
        {
            Confirm(Code, EmailText, null);
        }
    }
}