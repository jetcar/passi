using System;
using passi_android.utils.Services;

namespace AndroidTests.Tools;

internal class TestMainThreadService : IMainThreadService
{
    public void BeginInvokeOnMainThread(Action action)
    {
        Console.WriteLine("Dispatcher thread started.");
        action.Invoke();
    }
}