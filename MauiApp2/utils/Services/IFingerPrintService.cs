using MauiApp2.StorageModels;
using WebApiDto;

namespace MauiApp2.utils.Services
{
    public interface IFingerPrintService
    {
        void StartReadingConfirmRequest(NotificationDto message, AccountDb accountDb, Action<string> errorAction);
    }
}