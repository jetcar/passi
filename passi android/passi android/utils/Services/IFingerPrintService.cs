using System;
using WebApiDto;

namespace passi_android.utils.Services
{
    public interface IFingerPrintService
    {
        void StartReadingConfirmRequest(NotificationDto message, AccountDb accountDb, Action<string> errorAction);
    }
}