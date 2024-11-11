using MauiCommonServices;

namespace MauiApp1.utils
{
    public class MainThreadService : IMainThreadService
    {
        public void BeginInvokeOnMainThread(Action action)
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
    }

}