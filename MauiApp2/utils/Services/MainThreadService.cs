﻿using MauiCommonServices;
using MauiViewModels.utils.Services;

namespace MauiApp2.utils.Services
{
    public class MainThreadService : IMainThreadService
    {
        public void BeginInvokeOnMainThread(Action action)
        {
            MainThread.BeginInvokeOnMainThread(action);
        }
    }

}