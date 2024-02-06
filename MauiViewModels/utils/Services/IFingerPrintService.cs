using System;
using MauiViewModels.StorageModels;
using WebApiDto;

namespace MauiViewModels.utils.Services;

public interface IFingerPrintService
{
    void StartReadingConfirmRequest(NotificationDto message, AccountDb accountDb, Action<string> errorAction);
}