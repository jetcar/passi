using System;
using MauiViewModels.utils.Services;

namespace MauiTest.Tools;

internal class TestMainThreadService : IMainThreadService
{
    public void BeginInvokeOnMainThread(Action action)
    {
        Console.WriteLine("Dispatcher thread started.");
        action.Invoke();
    }
}