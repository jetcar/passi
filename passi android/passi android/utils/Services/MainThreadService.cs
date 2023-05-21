using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xamarin.Essentials;

namespace passi_android.utils.Services
{
    public class MainThreadService : IMainThreadService
    {
        public void BeginInvokeOnMainThread(Action action)
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
    }

    public interface IMainThreadService
    {
        void BeginInvokeOnMainThread(Action action);
    }
}