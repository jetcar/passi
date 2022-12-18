using System;
using System.Collections.Generic;
using System.Net;
using System.Timers;
using AppCommon;
using AppConfig;
using Newtonsoft.Json;
using passi_android.Tools;
using passi_android.utils;
using passi_android.utils.Certificate;
using WebApiDto;
using WebApiDto.Auth;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Color = WebApiDto.Auth.Color;

namespace passi_android.Notifications
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NotificationVerifyRequestView : ContentPage, IConfirmationView
    {
        private List<Color> possibleCodes = null;
        private Xamarin.Forms.Color _color1;
        private Xamarin.Forms.Color _color2;
        private Xamarin.Forms.Color _color3;
        private string _requesterName;
        private string _returnHost;
        private ValidationError _colorError = new ValidationError();
        private Timer _timer;
        private string _timeLeft;
        private bool _isButtonEnabled = true;
        private string _responseError;
        private static object locker = new object();
        private static NotificationVerifyRequestView _instance;

        public static NotificationVerifyRequestView Instance
        {
            get
            {
                lock (locker)
                {
                    if (_instance == null)
                        _instance = new NotificationVerifyRequestView();
                    return _instance;
                }
            }
        }

        private NotificationVerifyRequestView()
        {
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
            lock (locker)
            {
                _instance = null;
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Message != null)
            {
                var left = Message.ExpirationTime - DateTimeService.UtcNow;
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
        }

        public AccountDb Account { get; set; }

        private Xamarin.Forms.Color RandomColor()
        {
            var random = new Random();
            var randomIndex = random.Next(0, possibleCodes.Count - 1);
            var randomColor = possibleCodes[randomIndex];
            possibleCodes.Remove(randomColor);
            return GetColor(randomColor);
        }

        private Xamarin.Forms.Color GetColor(Color color)
        {
            switch (color)
            {
                case Color.yellow:
                    return Xamarin.Forms.Color.Yellow;

                case Color.green:
                    return Xamarin.Forms.Color.Green;

                case Color.red:
                    return Xamarin.Forms.Color.Red;

                case Color.blue:
                    return Xamarin.Forms.Color.Blue;
            }
            return Xamarin.Forms.Color.Black;
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

        public Xamarin.Forms.Color Color1
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

        public Xamarin.Forms.Color Color2
        {
            get => _color2;
            set
            {
                _color2 = value;
                OnPropertyChanged();
            }
        }

        public Xamarin.Forms.Color Color3
        {
            get => _color3;
            set
            {
                _color3 = value;
                OnPropertyChanged();
            }
        }

        public NotificationDto Message { get; set; }

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
                Navigation.PushModalSinglePage(new LoadingPage(() =>
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

                            var authorizeDto = new AuthorizeDto
                            {
                                SignedHash = signedGuid.Result,
                                PublicCertThumbprint = SecureRepository.GetAccount(Message.AccountGuid).Thumbprint,
                                SessionId = Message.SessionId
                            };
                            RestService.ExecutePostAsync(ConfigSettings.Authorize, authorizeDto).ContinueWith((response) =>
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
                    }));
            }
            else
            {
                Navigation.PushModalSinglePage(new ConfirmByPinView() { Message = Message });
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
            RestService.ExecuteAsync(ConfigSettings.CancelCheck + "?SessionId=" + Message.SessionId);
            Navigation.NavigateTop();
        }
    }
}