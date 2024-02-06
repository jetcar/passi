using System;

namespace MauiViewModels.utils.Services;

public interface IMainThreadService
{
    void BeginInvokeOnMainThread(Action action);
}