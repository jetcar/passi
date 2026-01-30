using System.ComponentModel;
using AppCommon;
using MauiCommonServices;
using MauiViewModels.utils.Services;
using MauiViewModels.utils.Services.Certificate;
using Microsoft.Extensions.DependencyInjection;

namespace MauiViewModels;

public class PassiBaseViewModel : BaseViewModel, INotifyPropertyChanged
{
    protected ISecureRepository _secureRepository;
    protected IRestService _restService;
    protected ICertHelper _certHelper;
    protected ICertificatesService _certificatesService;
    protected IDateTimeService _dateTimeService;
    protected IFingerPrintService _fingerPrintService;
    protected ISyncService _syncService;

    public PassiBaseViewModel()
    {
        _secureRepository = CommonApp.Services.GetService<ISecureRepository>();
        _restService = CommonApp.Services.GetService<IRestService>();
        _certHelper = CommonApp.Services.GetService<ICertHelper>();
        _certificatesService = CommonApp.Services.GetService<ICertificatesService>();
        _dateTimeService = CommonApp.Services.GetService<IDateTimeService>();
        _fingerPrintService = CommonApp.Services.GetService<IFingerPrintService>();
        _syncService = CommonApp.Services.GetService<ISyncService>();
    }

}