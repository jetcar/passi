using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace MauiCommonServices;

public class BaseViewModel : INotifyPropertyChanged
{
    public static INavigationService _navigationService;
    protected IMainThreadService _mainThreadService;
    protected IMySecureStorage _mySecureStorage;

    public BaseViewModel()
    {
        _navigationService = CommonApp.Services.GetService<INavigationService>();
        _mainThreadService = CommonApp.Services.GetService<IMainThreadService>();
        _mySecureStorage = CommonApp.Services.GetService<IMySecureStorage>();
    }

    public bool Appeared { get; private set; }

    public virtual void OnAppearing(object sender, EventArgs eventArgs)
    {
        Appeared = true;
    }

    public virtual void OnDisappearing(object sender, EventArgs eventArgs)
    {
        Appeared = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}