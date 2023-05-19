using System;
using Newtonsoft.Json;
using passi_android.utils;
using System.Net;
using WebApiDto;
using WebApiDto.SignUp;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Registration
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RegistrationConfirmation : ContentPage
    {
        private string _code = "";
        private string _responseError;
        private string _email;
        private ISecureRepository _secureRepository;
        IRestService _restService;
        private INavigationService Navigation;
        public RegistrationConfirmation(AccountDb account)
        {
            Navigation = App.Services.GetService<INavigationService>();
            Account = account;
            _secureRepository = App.Services.GetService<ISecureRepository>();
            _restService = App.Services.GetService<IRestService>();
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

        public AccountDb Account { get; set; }

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
                DeviceId = _secureRepository.GetDeviceId()
            };
            _restService.ExecutePostAsync(Account.Provider, Account.Provider.SignupCheck, signupConfirmationDto).ContinueWith((response) =>
            {
                if (response.Result.IsSuccessful)
                {
                    Account.IsConfirmed = true;
                    Account.Provider = Account.Provider;
                    _secureRepository.UpdateAccount(Account);
                    MainThread.BeginInvokeOnMainThread(() => { Navigation.PushModalSinglePage(new FinishConfirmation() { Code = Code, Account = Account }); });
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