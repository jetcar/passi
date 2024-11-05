using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MauiCommonServices;
using Timer = System.Timers.Timer;

namespace MauiViewModels.Tools
{
    public class LoadingViewModel : PassiBaseViewModel
    {
        private readonly Action _callBack;
        private Task _task;
        private readonly Timer _timer;

        public LoadingViewModel(Action callBack, int timeout = 30000)
        {
            _callBack = callBack;

            _timer = new Timer();
            _timer.Enabled = true;
            _timer.Interval = timeout;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!CommonApp.SkipLoadingTimer)
                _mainThreadService.BeginInvokeOnMainThread(() =>
                {
                    _navigationService.DisplayAlert("Something went wrong", "Something went wrong, redirecting to first page.", "Ok");
                    _navigationService.NavigateTop();
                });
        }

        public override void OnAppearing(object sender, EventArgs eventArgs)
        {
            this._task = Task.Run(() =>
            {
                _callBack.Invoke();
            });
            base.OnAppearing(sender, eventArgs);
        }

        public override void OnDisappearing(object sender, EventArgs eventArgs)
        {
            while (!_task.IsCompleted)
            {
                Thread.Sleep(1);
            }
            _task.Dispose();
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            _timer.Dispose();
            base.OnDisappearing(sender, eventArgs);
        }
    }
}