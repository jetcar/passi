using System.Timers;
using Timer = System.Timers.Timer;

namespace MauiApp2.Tools
{
    public partial class LoadingView : BaseContentPage
    {
        private readonly Action _callBack;
        private Task _task;
        private readonly Timer _timer;

        public LoadingView(Action callBack, int timeout = 30000)
        {
            _callBack = callBack;
            InitializeComponent();
            _timer = new Timer();
            _timer.Enabled = true;
            _timer.Interval = timeout;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!App.SkipLoadingTimer)
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