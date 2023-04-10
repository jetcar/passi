using AppCommon;
using passi_maui.Notifications;
using passi_maui.Registration;
using passi_maui.Tools;
using passi_maui.utils;
using WebApiDto;
using Color = WebApiDto.Auth.Color;

namespace passi_maui.Admin
{
    [QueryProperty("Account", "Account")]
    public partial class AdminPage : ContentPage
    {
        private string _cert64;
        private string _pass;
        private string _guid;
        private string _isFinished;
        private string _notificationToken;
        private string _deviceId;
        private AccountDb _account;

        public AdminPage()
        {
            InitializeComponent();
            BindingContext = this;

            NotificationToken = SecureStorage.GetAsync(StorageKeys.NotificationToken).Result;
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

        protected override void OnAppearing()
        {
            Cert64 = StringExtensions.Truncate(Account.PublicCertBinary, 10);
            Guid = Account.Guid.ToString();
            Pass = Account.Password;
            IsFinished = Account.IsConfirmed.ToString();
            DeviceId = SecureRepository.GetDeviceId();
            base.OnAppearing();
        }

        public string Cert64
        {
            get => _cert64;
            set
            {
                _cert64 = value;
                OnPropertyChanged();
            }
        }

        public string Pass
        {
            get => _pass;
            set
            {
                _pass = value;
                OnPropertyChanged();
            }
        }

        public string Guid
        {
            get => _guid;
            set
            {
                _guid = value;
                OnPropertyChanged();
            }
        }

        public string IsFinished
        {
            get => _isFinished;
            set
            {
                _isFinished = value;
                OnPropertyChanged();
            }
        }

        public string NotificationToken
        {
            get => _notificationToken;
            set
            {
                _notificationToken = value;
                OnPropertyChanged();
            }
        }

        public string DeviceId
        {
            get => _deviceId;
            set
            {
                _deviceId = value;
                OnPropertyChanged();
            }
        }

        private void ClearNotificationToken(object sender, EventArgs e)
        {
            SecureStorage.Remove(StorageKeys.NotificationToken);
            NotificationToken = null;
        }

        private void SaveNotificationToken(object sender, EventArgs e)
        {
            SecureStorage.SetAsync(StorageKeys.NotificationToken, NotificationToken);
        }

        private void MainPageclicked(object sender, EventArgs e)
        {
            Navigation.NavigateTop();
        }

        private void RegistrationConfirmation(object sender, EventArgs e)
        {
            ProviderDb provider = SecureRepository.GetProvider(Account.ProviderGuid);
            Account.Provider = provider;
            Navigation.PushModalSinglePage(new RegistrationConfirmation(), new Dictionary<string, object>() { { "Account", Account } });
        }

        private void EmptyView(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new AccountView(), new Dictionary<string, object>() { { "Account", Account } });
        }

        private void NotificationConfirmationView(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new NotificationVerifyRequestView(),new Dictionary<string, object>(){{"Message",new NotificationDto()
            {
                Sender = "sender",
                ConfirmationColor = Color.green,
                RandomString = System.Guid.NewGuid().ToString(),
                SessionId = System.Guid.NewGuid(),
                ExpirationTime = DateTime.UtcNow.AddSeconds(90),
                ReturnHost = "https://localhost"
            }}});
        }

        private void ConfirmByPinView(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new ConfirmByPinView(), new Dictionary<string, object>() { {"Message", new NotificationDto()
            {
                Sender = "sender", ConfirmationColor = Color.green, RandomString = System.Guid.NewGuid().ToString(), SessionId = System.Guid.NewGuid(), AccountGuid = Account.Guid, ExpirationTime = DateTime.UtcNow.AddSeconds(90), ReturnHost = "https://localhost"
            }}});
        }

        private void FinishConfirmation(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new FinishConfirmation(), new Dictionary<string, object>() { { "Code", "5" }, { "Account", Account }, { "EmailText", "your@email.com" } });
        }

        private void LoadingPage(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new LoadingPage(), new Dictionary<string, object>()
            {
                { "Action", new Action(() =>
                {
                    Thread.Sleep(10000);
                    Navigation.PopModal();
                })
            }
        });
        }

        private void ClearProvider(object sender, EventArgs e)
        {
            Account.Provider = null;
            Account.ProviderGuid = null;
            SecureRepository.UpdateAccount(Account);
        }
    }
}