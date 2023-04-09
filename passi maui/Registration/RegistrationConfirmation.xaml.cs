using System.Net;
using Android.Content;
using Android.Views.InputMethods;
using Microsoft.Maui.Controls.Compatibility;
using Newtonsoft.Json;
using passi_maui.utils;
using WebApiDto;
using WebApiDto.SignUp;

namespace passi_maui.Registration
{
    [QueryProperty("Account", "Account")]
    public partial class RegistrationConfirmation : ContentPage
    {
        private string _code = "";
        private string _responseError;
        private string _email;
        private AccountDb _account;

        public RegistrationConfirmation()
        {
            InitializeComponent();
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            Email = Account.Email;
            base.OnAppearing();
        }

        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                OnPropertyChanged();
                ResponseError = "";
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanging();
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

        public AccountDb Account
        {
            get => _account;
            set
            {
                if (Equals(value, _account)) return;
                _account = value;
                OnPropertyChanged();
            }
        }

        private void NumbersPad_OnNumberClicked(string value)
        {
            if (value == "confirm")
            {
                CheckCodeAndNavigateForward();
                return;
            }
            if (value == "del")
            {
                if (Code.Length > 0)
                    Code = Code.Substring(0, Code.Length - 1);
                return;
            }
            Code += value;
            if (Code.Length >= 6)
            {
                CheckCodeAndNavigateForward();
            }
        }

        private void CheckCodeAndNavigateForward()
        {
            ResponseError = "";
            var signupConfirmationDto = new SignupConfirmationDto()
            {
                Code = Code,
                Email = Email,
                DeviceId = SecureRepository.GetDeviceId()
            };
            RestService.ExecutePostAsync(Account.Provider, Account.Provider.SignupCheck, signupConfirmationDto).ContinueWith((response) =>
            {
                if (response.Result.IsSuccessful)
                {
                    Account.IsConfirmed = true;
                    Account.Provider = Account.Provider;
                    SecureRepository.UpdateAccount(Account);
                    MainThread.BeginInvokeOnMainThread(() => { Navigation.PushModalSinglePage(new FinishConfirmation(),new Dictionary<string, object>() { {"Code", Code},{"Account", Account} }); });
                }
                else if (!response.Result.IsSuccessful && response.Result.StatusCode == HttpStatusCode.BadRequest)
                {
                    ResponseError = JsonConvert.DeserializeObject<ApiResponseDto<string>>(response.Result.Content).Message;
                }
                else
                {
                    ResponseError = "Network error. Try again";
                }
            });
        }

        private void CancelButton_OnClicked(object sender, EventArgs e)
        {
            Navigation.NavigateTop();
        }
    }
}