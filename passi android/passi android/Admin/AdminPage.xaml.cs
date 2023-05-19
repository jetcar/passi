﻿using System;
using System.Threading;
using AppCommon;
using passi_android.Notifications;
using passi_android.Registration;
using passi_android.Tools;
using passi_android.utils;
using WebApiDto;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Color = WebApiDto.Auth.Color;
using passi_android.Menu;

namespace passi_android.Admin
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AdminPage : ContentPage
    {
        private string _cert64;
        private string _salt;
        private string _guid;
        private string _isFinished;
        private string _notificationToken;
        private string _deviceId;
        ISecureRepository _secureRepository;
        IMySecureStorage _mySecureStorage;
        INavigationService Navigation;
        public AdminPage(AccountDb account)
        {
            _secureRepository = App.Services.GetService<ISecureRepository>();
            _mySecureStorage = App.Services.GetService<IMySecureStorage>();
            Navigation = App.Services.GetService<INavigationService>();
            this.Account = account;
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
            Navigation.NavigateTop();
        }

        private void RegistrationConfirmation(object sender, EventArgs e)
        {
            ProviderDb provider = _secureRepository.GetProvider(Account.ProviderGuid);
            Account.Provider = provider;
            Navigation.PushModalSinglePage(new RegistrationConfirmation(Account));
        }

        private void EmptyView(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new AccountView(Account));
        }

        private void NotificationConfirmationView(object sender, EventArgs e)
        {
            NotificationVerifyRequestView.Instance.Message = new NotificationDto()
            {
                Sender = "sender",
                ConfirmationColor = Color.green,
                RandomString = System.Guid.NewGuid().ToString(),
                SessionId = System.Guid.NewGuid(),
                ExpirationTime = DateTime.UtcNow.AddSeconds(90),
                ReturnHost = "https://localhost"
            };
            Navigation.PushModalSinglePage(NotificationVerifyRequestView.Instance);
        }

        private void ConfirmByPinView(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new ConfirmByPinView() { Message = new NotificationDto() { Sender = "sender", ConfirmationColor = Color.green, RandomString = System.Guid.NewGuid().ToString(), SessionId = System.Guid.NewGuid(), AccountGuid = Account.Guid, ExpirationTime = DateTime.UtcNow.AddSeconds(90), ReturnHost = "https://localhost" } });
        }

        private void FinishConfirmation(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new FinishConfirmation() { Code = "5", Account = Account, EmailText = "your@email.com" });
        }

        private void LoadingPage(object sender, EventArgs e)
        {
            Navigation.PushModalSinglePage(new LoadingPage(new Action(() =>
            {
                Thread.Sleep(10000);
                Navigation.PopModal();
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