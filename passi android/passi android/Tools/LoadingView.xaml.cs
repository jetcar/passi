using passi_android.utils.Services;
using System;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace passi_android.Tools
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoadingView : ContentPage
    {
        private readonly Action _callBack;
        private Task _task;
        private readonly Timer _timer;

        private INavigationService _navigationService;
        private IMainThreadService _mainThreadService;
        public LoadingView(Action callBack)
        {
            _navigationService = App.Services.GetService<INavigationService>();
            _mainThreadService = App.Services.GetService<IMainThreadService>();
            _callBack = callBack;
            if (!App.IsTest)
                InitializeComponent();
            _timer = new Timer();
            _timer.Enabled = true;
            _timer.Interval = 30000;
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
            _task.Dispose();
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            _timer.Dispose();
            base.OnDisappearing();
        }
    }
}