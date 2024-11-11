namespace MauiCommonServices;

public interface IMainThreadService
{
    void BeginInvokeOnMainThread(Action action);
}

