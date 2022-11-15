using System;
using System.Net;
using System.Net.Mail;
using AppCommon;
using AppConfig;
using Newtonsoft.Json;
using passi_android.Registration;
using passi_android.Tools;
using passi_android.utils;
using RestSharp;
using WebApiDto;
using WebApiDto.SignUp;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddAccountPage : ContentPage
    {
        private string _emailText = "";
        private ValidationError _emailError;
        private string _responseError;

        public string EmailText
        {
            get
            {
                return _emailText;
            }
            set
            {
                _emailText = value;
                ResponseError = "";
            }
        }

        public ValidationError EmailError
        {
            get => _emailError;
            set
            {
                _emailError = value;
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

        public AddAccountPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        public void Button_OnClicked(object sender, EventArgs e)
        {
            var button = sender as VisualElement;
            button.IsEnabled = false;

            if (!IsValid(EmailText))
            {
                button.IsEnabled = true;
                return;
            }

            var account = new AccountDb() { Email = EmailText, DeviceId = SecureRepository.GetDeviceId(), Guid = Guid.NewGuid() };
            var signupDto = new SignupDto()
            {
                Email = EmailText,
                UserGuid = account.Guid,
                DeviceId = SecureRepository.GetDeviceId()
            };

            Navigation.PushModalSinglePage(new LoadingPage(new Action(() =>
            {
                RestService.ExecutePostAsync(ConfigSettings.SignupPath, signupDto).ContinueWith((response) =>
                {
                    if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Navigation.PopModal().ContinueWith((task =>
                            {
                                var responseError = JsonConvert.DeserializeObject<ApiResponseDto<string>>(response.Result.Content);
                                ResponseError = responseError.Message;
                            }));
                        });
                    }
                    else if (response.Result.IsSuccessful)
                    {
                        SecureRepository.AddAccount(account);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            Navigation.PushModalSinglePage(new NavigationPage(new RegistrationConfirmation()
                            { Account = account }));
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
            })));
            button.IsEnabled = true;
        }

        private bool IsValid(string emailText)
        {
            var isValid = false;
            try
            {
                string email = emailText;
                var mail = new MailAddress(email);
                bool isValidEmail = mail.Host.Contains(".");
                isValid = isValidEmail;
            }
            catch (Exception)
            {
                isValid = false;
            }

            EmailError = new ValidationError() { HasError = !isValid, Text = !isValid ? "invalid Email" : "" };
            return isValid;
        }

        private void CancelButton_OnClicked(object sender, EventArgs e)
        {
            Navigation.NavigateTop();
        }
    }
}