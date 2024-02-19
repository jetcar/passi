using System;
using System.Threading;
using AppCommon;
using MauiViewModels.Main;
using MauiViewModels.Notifications;
using MauiViewModels.Registration;
using MauiViewModels.StorageModels;
using MauiViewModels.Tools;
using MauiViewModels.utils;
using WebApiDto;

namespace MauiViewModels.Admin
{
    public class AdminView : BaseViewModel
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

            NotificationToken = _mySecureStorage.GetAsync(StorageKeys.NotificationToken).Result;
        }

        public AccountDb Account { get; set; }

        public override void OnAppearing(object sender, EventArgs eventArgs)
        {
            Cert64 = StringExtensions.Truncate(Account.PublicCertBinary, 10);
            Guid = Account.Guid.ToString();
            Salt = Account.Salt;
            IsFinished = Account.IsConfirmed.ToString();
            DeviceId = _secureRepository.GetDeviceId();
            base.OnAppearing(sender, eventArgs);
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
            _navigationService.PushModalSinglePage(new RegistrationConfirmationViewModel(Account));
        }

        private void EmptyView(object sender, EventArgs e)
        {
            _navigationService.PushModalSinglePage(new AccountViewModel(Account));
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
            _navigationService.PushModalSinglePage(new NotificationVerifyRequestViewModel(message));
        }

        private void ConfirmByPinView(object sender, EventArgs e)
        {
            _navigationService.PushModalSinglePage(new ConfirmByPinViewModel() { Message = new NotificationDto() { Sender = "sender", ConfirmationColor = WebApiDto.Auth.Color.green, RandomString = System.Guid.NewGuid().ToString(), SessionId = System.Guid.NewGuid(), AccountGuid = Account.Guid, ExpirationTime = DateTime.UtcNow.AddSeconds(90), ReturnHost = "https://localhost" } });
        }

        private void FinishConfirmation(object sender, EventArgs e)
        {
            _navigationService.PushModalSinglePage(new FinishConfirmationViewModel() { Code = "5", Account = Account, EmailText = "your@email.com" });
        }

        private void LoadingPage(object sender, EventArgs e)
        {
            _navigationService.PushModalSinglePage(new LoadingViewModel(new Action(() =>
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