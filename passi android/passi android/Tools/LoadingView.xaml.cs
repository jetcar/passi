using passi_android.utils.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Timer = System.Timers.Timer;

namespace passi_android.Tools
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoadingView : BaseContentPage
    {
        private readonly Action _callBack;
        private Task _task;
        private readonly Timer _timer;

        public LoadingView(Action callBack, int timeout = 30000)
        {
            _callBack = callBack;
            if (!App.IsTest)
                InitializeComponent();
            _timer = new Timer();
            _timer.Enabled = true;
            _timer.Interval = timeout;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _mainThreadService.BeginInvokeOnMainThread(() =>
            {
                DisplayAlert("Something went wrong", "Something went wrong, redirecting to first page.", "Ok");
                _navigationService.NavigateTop();
            });
        }

        protected override void OnAppearing()
        {
            this._task = Task.Run(() =>
            {
                _callBack.Invoke();
            });
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            while (!_task.IsCompleted)
            {
                Thread.Sleep(1);
            }
            _task.Dispose();
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            _timer.Dispose();
            base.OnDisappearing();
        }
    }
}