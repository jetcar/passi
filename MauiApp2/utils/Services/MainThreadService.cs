namespace MauiApp2.utils.Services
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