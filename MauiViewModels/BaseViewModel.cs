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
        _navigationService = App.Services.GetService<INavigationService>();
        _mainThreadService = App.Services.GetService<IMainThreadService>();
        _secureRepository = App.Services.GetService<ISecureRepository>();
        _restService = App.Services.GetService<IRestService>();
        _certHelper = App.Services.GetService<ICertHelper>();
        _certificatesService = App.Services.GetService<ICertificatesService>();
        _mySecureStorage = App.Services.GetService<IMySecureStorage>();
        _dateTimeService = App.Services.GetService<IDateTimeService>();
        _fingerPrintService = App.Services.GetService<IFingerPrintService>();
        _syncService = App.Services.GetService<ISyncService>();
    }

    public bool Appeared { get; private set; }

    public virtual void OnAppearing()
    {
        Appeared = true;
    }

    public virtual void OnDisappearing()
    {
        Appeared = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}