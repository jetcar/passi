using System.Timers;
using Microsoft.Maui.LifecycleEvents;
using passi_maui.utils;
using Timer = System.Timers.Timer;

namespace passi_maui.Tools
{
    [QueryProperty("Action", "Action")]
    public partial class LoadingPage : ContentPage
    {
        public Action Action { get; set; }
        private Task _task;
        private readonly Timer _timer;

        public LoadingPage()
        {
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
                if (_task != null)
                    _task.Dispose();
                _timer.Elapsed -= _timer_Elapsed;
                _timer.Stop();
                _timer.Dispose();
                DisplayAlert("Something went wrong", "Something went wrong, redirecting to first page.", "Ok");
                Navigation.NavigateTop();

            });
        }

        protected override void OnAppearing()
        {
            this._task = Task.Run(() =>
            {
                Action.Invoke();

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