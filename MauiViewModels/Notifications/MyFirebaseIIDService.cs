using System.Threading;
using MauiCommonServices;
using MauiViewModels.utils;
using MauiViewModels.utils.Services;
using Microsoft.Extensions.DependencyInjection;
using WebApiDto;

namespace MauiViewModels.Notifications
{
    public class MyFirebaseIIDService
    {
        public static void SendRegistrationToServer(string token)
        {
            var secureRepository = CommonApp.Services.GetService<ISecureRepository>();
            var restService = CommonApp.Services.GetService<IRestService>();
            var mySecureStorage = CommonApp.Services.GetService<IMySecureStorage>();

            var deviceTokenUpdateDto = new DeviceTokenUpdateDto() { DeviceId = secureRepository.GetDeviceId(), Token = token, Platform = "Android" };
            foreach (var provider in secureRepository.LoadProviders().Result)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    var result = restService.ExecutePostAsync(provider, provider.TokenUpdate, deviceTokenUpdateDto)
                        .Result;
                    if (!result.IsSuccessful)
                    {
                        do
                        {
                            Thread.Sleep(10000);
                            result = restService.ExecutePostAsync(provider, provider.TokenUpdate, deviceTokenUpdateDto)
                                .Result;
                        } while (!result.IsSuccessful);

                        mySecureStorage.SetAsync(StorageKeys.NotificationToken, token);
                    }
                    else
                    {
                        mySecureStorage.SetAsync(StorageKeys.NotificationToken, token);
                    }
                });
            }
        }
    }
}