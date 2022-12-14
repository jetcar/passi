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
using WebApiDto.Certificate;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using CertHelper = passi_android.utils.Certificate.CertHelper;
using Certificates = passi_android.utils.Certificate.Certificates;
using Color = Xamarin.Forms.Color;

namespace passi_android
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class UpdateCertificate : ContentPage
    {
        private string _pin1Masked;
        private string _pin2Masked;
        private string _pinOldMasked;
        private string _pin1 = "";
        private string _pin2 = "";
        private string _pinOld = "";
        private Color _pin1Color;
        private Color _pin2Color;
        private Color _pinOldColor;
        private string _responseError;
        private ValidationError _pin1Error = new ValidationError();
        private ValidationError _pin2Error = new ValidationError();
        private ValidationError _pinOldError = new ValidationError();

        private readonly int MinPinLenght = 4;

        public UpdateCertificate()
        {
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

        public string PinOld
        {
            get => _pinOld;
            set
            {
                _pinOld = value;
                PinOldMasked = new string(_pinOld.ToList().Select(x => '*').ToArray());
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

        private void Confirm(string pinNew, string pinOld)
        {
            Navigation.PushModalSinglePage(new LoadingPage(() =>
            {
                GenerateCert(pinNew, pinOld).ContinueWith(certDto =>
                {
                    if (certDto != null)
                    {
                        RestService.ExecutePostAsync(Account.Provider,Account.Provider.UpdateCertificate, certDto.Result).ContinueWith((response) =>
                        {
                            if (response.Result.IsSuccessful)
                            {
                                Account.Password = NewAccount.Password;
                                Account.PrivateCertBinary = NewAccount.PrivateCertBinary;
                                Account.pinLength = NewAccount.pinLength;
                                Account.Thumbprint = NewAccount.Thumbprint;
                                Account.ValidFrom = NewAccount.ValidFrom;
                                Account.ValidTo = NewAccount.ValidTo;
                                Account.PublicCertBinary = NewAccount.PublicCertBinary;

                                SecureRepository.UpdateAccount(Account);
                                SecureRepository.AddfingerPrintKey(Account.Guid, pinNew).GetAwaiter().GetResult();

                                MainThread.BeginInvokeOnMainThread(() => { Navigation.NavigateTop(); });
                            }
                            if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                            {
                                Navigation.PopModal().ContinueWith((task) =>
                                {
                                    ResponseError = JsonConvert
                                        .DeserializeObject<ApiResponseDto<string>>(response.Result.Content).Message;
                                });
                            }
                            else
                            {
                                Navigation.PopModal().ContinueWith((task) =>
                                {
                                    ResponseError = "Network error. Try again";
                                });
                            }
                        });
                    }
                });
            }));
        }

        private async Task<CertificateDto> GenerateCert(string pinNew, string pinOld)
        {
            var cert = new CertificateDto();
            cert.ParentCertThumbprint = Account.Thumbprint;
            return await Certificates.GenerateCertificate(Account.Email, pinNew).ContinueWith(certificate =>
            {
                var certificateBase64 = Convert.ToBase64String(certificate.Result.Item3); //importable certificate
                var publicCertHelper = CertHelper.ConvertToPublicCertificate(certificate.Result.Item1);

                try
                {
                    cert.ParentCertHashSignature = CertHelper.Sign(Account.Guid, pinOld, publicCertHelper.BinaryData).Result;
                }
                catch (Exception e)
                {
                    PinOldError.HasError = true;
                    PinOldError.Text = "Invalid old pin";
                    SetTextBox(0);
                    return null;
                }
                cert.PublicCert = publicCertHelper.BinaryData;
                cert.DeviceId = SecureRepository.GetDeviceId();
                NewAccount.Password = certificate.Result.Item2;
                NewAccount.PrivateCertBinary = certificateBase64;
                NewAccount.pinLength = Pin1.Length;
                NewAccount.Thumbprint = publicCertHelper.Thumbprint;
                NewAccount.ValidFrom = publicCertHelper.NotBefore.Value;
                NewAccount.ValidTo = publicCertHelper.NotAfter.Value;
                NewAccount.PublicCertBinary = publicCertHelper.BinaryData;
                return cert;
            });
        }

        private int currentfieldIndex = 0;
        private AccountDb _newAccount = new AccountDb();

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

        public AccountDb NewAccount
        {
            get => _newAccount;
            set => _newAccount = value;
        }

        private void NumbersPad_OnNumberClicked(string value)
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

                    if (Pin1 != Pin2)
                    {
                        SetTextBox(2);
                        //error
                        Pin2Error.HasError = true;
                        Pin2Error.Text = "Pin2 doesn't match with Pin1";

                        return;
                    }

                    Confirm(Pin1, PinOld);
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
                        PinOld = PinOld.Substring(0, PinOld.Length - 1);
                if (currentfieldIndex == 1)
                    if (Pin1.Length > 0)
                        Pin1 = Pin1.Substring(0, Pin1.Length - 1);
                if (currentfieldIndex == 2)
                    if (Pin2.Length > 0)
                        Pin2 = Pin1.Substring(0, Pin2.Length - 1);
                return;
            }
            if (currentfieldIndex == 0)
                PinOld += value;
            if (currentfieldIndex == 1)
                Pin1 += value;
            if (currentfieldIndex == 2)
                Pin2 += value;
        }

        private void ClearPin1_OnClicked(object sender, EventArgs e)
        {
            Pin1 = "";
            SetTextBox(1);
        }

        private void ClearPin2_OnClicked(object sender, EventArgs e)
        {
            Pin2 = "";
            SetTextBox(2);
        }

        private void ClearPinOld_OnClicked(object sender, EventArgs e)
        {
            PinOld = "";
            SetTextBox(0);
        }

        private void Button_Cancel(object sender, EventArgs e)
        {
            var element = sender as VisualElement;
            element.IsEnabled = false;

            Navigation.PopModal();
            element.IsEnabled = true;
        }
    }
}