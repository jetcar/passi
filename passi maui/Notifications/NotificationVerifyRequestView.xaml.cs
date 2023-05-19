using System.Net;
using System.Timers;
using AppCommon;
using Newtonsoft.Json;
using passi_maui.Tools;
using passi_maui.utils;
using passi_maui.utils.Certificate;
using WebApiDto;
using WebApiDto.Auth;
using Color = WebApiDto.Auth.Color;
using Timer = System.Timers.Timer;

namespace passi_maui.Notifications
{
    [QueryProperty("Message", "Message")]
    public partial class NotificationVerifyRequestView : ContentPage, IConfirmationView
    {
        private List<Color> possibleCodes = null;
        private Microsoft.Maui.Graphics.Color  _color1;
        private Microsoft.Maui.Graphics.Color  _color2;
        private Microsoft.Maui.Graphics.Color  _color3;
        private string _requesterName;
        private string _returnHost;
        private ValidationError _colorError = new ValidationError();
        private Timer _timer;
        private string _timeLeft;
        private bool _isButtonEnabled = true;
        private string _responseError;
        private AccountDb _account;
        private NotificationDto _message;
        private IDateTimeService _dateTimeService;

        public NotificationVerifyRequestView()
        {
            _dateTimeService = App.Services.GetService<IDateTimeService>();

            InitializeComponent();
            BindingContext = this;
            _timer = new Timer();
            _timer.Enabled = true;
            _timer.Interval = 500;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        protected override void OnDisappearing()
        {
            _timer.Elapsed -= _timer_Elapsed;

            _timer.Stop();
            _timer.Dispose();
            base.OnDisappearing();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Message != null)
            {
                var left = Message.ExpirationTime - _dateTimeService.UtcNow;
                var leftTotalSeconds = ((int)left.TotalSeconds);
                TimeLeft = leftTotalSeconds.ToString();
                if (leftTotalSeconds <= 0)
                {
                    _timer.Stop();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Application.Current.MainPage.DisplayAlert("Timeout", "Session Expired", "Ok");

                        Navigation.NavigateTop();
                    });
                }
            }
        }

        protected override void OnAppearing()
        {
            this.Account = SecureRepository.GetAccount(Message.AccountGuid);
            RequesterName = Message.Sender;
            ReturnHost = Message.ReturnHost;

            possibleCodes = new List<Color>()
            {
                Color.blue,
                Color.green,
                Color.red,
                Color.yellow
            };
            var random = new Random();
            var correctIndex = random.Next(1, 3);
            possibleCodes.Remove(Message.ConfirmationColor);
            switch (correctIndex)
            {
                case 1:
                    Color1 = GetColor(Message.ConfirmationColor);
                    Color2 = RandomColor();
                    Color3 = RandomColor();
                    break;

                case 2:
                    Color1 = RandomColor();
                    Color2 = GetColor(Message.ConfirmationColor);
                    Color3 = RandomColor();
                    break;

                case 3:
                    Color1 = RandomColor();
                    Color2 = RandomColor();
                    Color3 = GetColor(Message.ConfirmationColor);
                    break;
            }

            base.OnAppearing();
            App.CancelNotifications.Invoke();
            SecureRepository.ReleaseSessionKey(Message.SessionId);
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

        private Microsoft.Maui.Graphics.Color RandomColor()
        {
            var random = new Random();
            var randomIndex = random.Next(0, possibleCodes.Count - 1);
            var randomColor = possibleCodes[randomIndex];
            possibleCodes.Remove(randomColor);
            return GetColor(randomColor);
        }

        private Microsoft.Maui.Graphics.Color GetColor(Color color)
        {
            switch (color)
            {
                case Color.yellow:
                    return Colors.Yellow;

                case Color.green:
                    return Colors.Green;

                case Color.red:
                    return Colors.Red;

                case Color.blue:
                    return Colors.Blue;
            }
            return Colors.Black;
        }

        public string RequesterName
        {
            get => _requesterName;
            set
            {
                _requesterName = value;
                OnPropertyChanged();
            }
        }

        public string ReturnHost
        {
            get => _returnHost;
            set
            {
                _returnHost = value;
                OnPropertyChanged();
            }
        }

        public Microsoft.Maui.Graphics.Color  Color1
        {
            get => _color1;
            set
            {
                _color1 = value;
                OnPropertyChanged();
            }
        }

        public bool IsButtonEnabled
        {
            get => _isButtonEnabled;
            set
            {
                _isButtonEnabled = value;
                OnPropertyChanged();
            }
        }

        public Microsoft.Maui.Graphics.Color  Color2
        {
            get => _color2;
            set
            {
                _color2 = value;
                OnPropertyChanged();
            }
        }

        public Microsoft.Maui.Graphics.Color  Color3
        {
            get => _color3;
            set
            {
                _color3 = value;
                OnPropertyChanged();
            }
        }

        public NotificationDto Message
        {
            get => _message;
            set
            {
                if (Equals(value, _message)) return;
                _message = value;
                OnPropertyChanged();
            }
        }

        public ValidationError ColorError
        {
            get => _colorError;
            set
            {
                _colorError = value;
                OnPropertyChanged();
            }
        }

        public string TimeLeft
        {
            get => _timeLeft;
            set
            {
                _timeLeft = value;
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

        private void ImageButton1_OnClicked(object sender, EventArgs e)
        {
            if (Color1 == GetColor(Message.ConfirmationColor))
            {
                Process();
            }
            else
            {
                IsButtonEnabled = false;
                ColorError.HasError = true;
                ColorError.Text = "Invalid confirmation color. Go back and try again.";
            }
        }

        private void Process()
        {
            if (Account.pinLength == 0)
            {
                Navigation.PushModalSinglePage(new LoadingPage(),new Dictionary<string, object>() { {"Action",() =>
                {
                    CertHelper.Sign(Message.AccountGuid, null, Message.RandomString).ContinueWith(signedGuid =>
                    {
                        if (signedGuid.IsFaulted)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Navigation.PopModal().ContinueWith((task =>
                                {
                                    ResponseError = "Certificate Error:" + signedGuid.Exception.Message;
                                }));
                            });

                            return;
                        }

                        var accountDb = SecureRepository.GetAccount(Message.AccountGuid);
                        accountDb.Provider = SecureRepository.GetProvider(accountDb.ProviderGuid);
                        var authorizeDto = new AuthorizeDto
                        {
                            SignedHash = signedGuid.Result,
                            PublicCertThumbprint = accountDb.Thumbprint,
                            SessionId = Message.SessionId
                        };
                        RestService.ExecutePostAsync(accountDb.Provider, accountDb.Provider.Authorize, authorizeDto).ContinueWith((response) =>
                        {
                            if (response.Result.IsSuccessful)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    App.CloseApp.Invoke();
                                });
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
                }}});
            }
            else
            {
                Navigation.PushModalSinglePage(new ConfirmByPinView(),new Dictionary<string, object>() { {"Message", Message} });
            }
        }

        private void ImageButton2_OnClicked(object sender, EventArgs e)
        {
            if (Color2 == GetColor(Message.ConfirmationColor))
                Process();
            else
            {
                IsButtonEnabled = false;
                ColorError.HasError = true;
                ColorError.Text = "Invalid confirmation color. Go back and try again.";
            }
        }

        private void ImageButton3_OnClicked(object sender, EventArgs e)
        {
            if (Color3 == GetColor(Message.ConfirmationColor))
                Process();
            else
            {
                IsButtonEnabled = false;
                ColorError.HasError = true;
                ColorError.Text = "Invalid confirmation color. Go back and try again.";
            }
        }

        private void Cancel_OnClicked(object sender, EventArgs e)
        {
            var accountDb = SecureRepository.GetAccount(Message.AccountGuid);
            if (accountDb != null)
                accountDb.Provider = SecureRepository.GetProvider(accountDb.ProviderGuid);
            RestService.ExecuteAsync(accountDb.Provider, accountDb.Provider.CancelCheck + "?SessionId=" + Message.SessionId);
            Navigation.NavigateTop();
        }
    }
}