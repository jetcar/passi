using AppCommon;
using MauiApp2.Main;
using MauiApp2.Notifications;
using MauiApp2.StorageModels;
using MauiApp2.Tools;
using MauiApp2.utils;
using WebApiDto;
using FinishConfirmationView = MauiApp2.Registration.FinishConfirmationView;
using RegistrationConfirmationView = MauiApp2.Registration.RegistrationConfirmationView;

namespace MauiApp2.Admin
{
    public partial class AdminView : BaseContentPage
    {
        private string _cert64;
        private string _salt;
        private string _guid;
        private string _isFinished;
        private string _notificationToken;
        private string _deviceId;

        public AdminView(AccountDb account)
        {
            this.Account = account;
            if (!App.IsTest)
                InitializeComponent();
            BindingContext = this;
            NotificationToken = _mySecureStorage.GetAsync(StorageKeys.NotificationToken).Result;
        }

        public AccountDb Account { get; set; }

        protected override void OnAppearing()
        {
            Cert64 = StringExtensions.Truncate(Account.PublicCertBinary, 10);
            Guid = Account.Guid.ToString();
            Salt = Account.Salt;
            IsFinished = Account.IsConfirmed.ToString();
            DeviceId = _secureRepository.GetDeviceId();
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

        public string Salt
        {
            get => _salt;
            set
            {
                _salt = value;
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
            _mySecureStorage.Remove(StorageKeys.NotificationToken);
            NotificationToken = null;
        }

        private void SaveNotificationToken(object sender, EventArgs e)
        {
            _mySecureStorage.SetAsync(StorageKeys.NotificationToken, NotificationToken);
        }

        private void MainPageclicked(object sender, EventArgs e)
        {
            _navigationService.NavigateTop();
        }

        private void RegistrationConfirmation(object sender, EventArgs e)
        {
            ProviderDb provider = _secureRepository.GetProvider(Account.ProviderGuid);
            Account.Provider = provider;
            _navigationService.PushModalSinglePage(new RegistrationConfirmationView(Account));
        }

        private void EmptyView(object sender, EventArgs e)
        {
            _navigationService.PushModalSinglePage(new AccountView(Account));
        }

        private void NotificationConfirmationView(object sender, EventArgs e)
        {
            var message = new NotificationDto()
            {
                Sender = "sender",
                ConfirmationColor = WebApiDto.Auth.Color.green,
                RandomString = System.Guid.NewGuid().ToString(),
                SessionId = System.Guid.NewGuid(),
                ExpirationTime = DateTime.UtcNow.AddSeconds(90),
                ReturnHost = "https://localhost"
            };
            _navigationService.PushModalSinglePage(new NotificationVerifyRequestView(message));
        }

        private void ConfirmByPinView(object sender, EventArgs e)
        {
            _navigationService.PushModalSinglePage(new ConfirmByPinView() { Message = new NotificationDto() { Sender = "sender", ConfirmationColor = WebApiDto.Auth.Color.green, RandomString = System.Guid.NewGuid().ToString(), SessionId = System.Guid.NewGuid(), AccountGuid = Account.Guid, ExpirationTime = DateTime.UtcNow.AddSeconds(90), ReturnHost = "https://localhost" } });
        }

        private void FinishConfirmation(object sender, EventArgs e)
        {
            _navigationService.PushModalSinglePage(new FinishConfirmationView() { Code = "5", Account = Account, EmailText = "your@email.com" });
        }

        private void LoadingPage(object sender, EventArgs e)
        {
            _navigationService.PushModalSinglePage(new LoadingView(new Action(() =>
            {
                Thread.Sleep(10000);
                _navigationService.PopModal();
            })));
        }

        private void ClearProvider(object sender, EventArgs e)
        {
            Account.Provider = null;
            Account.ProviderGuid = null;
            _secureRepository.UpdateAccount(Account);
        }
    }
}