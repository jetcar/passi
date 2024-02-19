using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AppCommon;
using MauiViewModels.utils.Services;
using MauiViewModels.utils.Services.Certificate;
using Microsoft.Extensions.DependencyInjection;

namespace MauiViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
    public static INavigationService _navigationService;
    protected IMainThreadService _mainThreadService;
    protected ISecureRepository _secureRepository;
    protected IRestService _restService;
    protected ICertHelper _certHelper;
    protected ICertificatesService _certificatesService;
    protected IMySecureStorage _mySecureStorage;
    protected IDateTimeService _dateTimeService;
    protected IFingerPrintService _fingerPrintService;
    protected ISyncService _syncService;

    public BaseViewModel()
    {
        _navigationService = CommonApp.Services.GetService<INavigationService>();
        _mainThreadService = CommonApp.Services.GetService<IMainThreadService>();
        _secureRepository = CommonApp.Services.GetService<ISecureRepository>();
        _restService = CommonApp.Services.GetService<IRestService>();
        _certHelper = CommonApp.Services.GetService<ICertHelper>();
        _certificatesService = CommonApp.Services.GetService<ICertificatesService>();
        _mySecureStorage = CommonApp.Services.GetService<IMySecureStorage>();
        _dateTimeService = CommonApp.Services.GetService<IDateTimeService>();
        _fingerPrintService = CommonApp.Services.GetService<IFingerPrintService>();
        _syncService = CommonApp.Services.GetService<ISyncService>();
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