using System.ComponentModel;
using AppCommon;
using MauiCommonServices;
using Microsoft.Extensions.DependencyInjection;

namespace ChatViewModel;


public class ChatBaseViewModel : BaseViewModel, INotifyPropertyChanged
{
    protected IDateTimeService _dateTimeService;

    public ChatBaseViewModel()
    {
        _dateTimeService = CommonApp.Services.GetService<IDateTimeService>();
    }

}