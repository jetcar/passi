using passi_android.utils;
using System;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Tools
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoadingPage : ContentPage
    {
        private readonly Action _callBack;
        private Task _task;
        private readonly Timer _timer;

        public LoadingPage(Action callBack)
        {
            _callBack = callBack;
            InitializeComponent();
            _timer = new Timer();
            _timer.Enabled = true;
            _timer.Interval = 30000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DisplayAlert("Something went wrong", "Something went wrong, redirecting to first page.", "Ok");
                Navigation.NavigateTop();
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
            _task.Dispose();
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            _timer.Dispose();
            base.OnDisappearing();
        }
    }
}