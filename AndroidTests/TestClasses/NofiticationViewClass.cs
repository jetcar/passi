using System;
using System.Threading;
using AndroidTests.Tools;
using AppConfig;
using NUnit.Framework;
using passi_android;
using passi_android.Notifications;
using passi_android.Tools;
using WebApiDto;
using Xamarin.Forms;
using Color = WebApiDto.Auth.Color;

namespace AndroidTests.TestClasses;

public class NofiticationViewClass
{
    public static NotificationVerifyRequestView OpenNotificationPage(Guid accountGuid, Color color, int timeoutSeconds)
    {
        TestBase.Navigation.PushModalSinglePage(new NotificationVerifyRequestView(new NotificationDto() { AccountGuid = accountGuid, ConfirmationColor = color, ExpirationTime = DateTime.UtcNow.AddSeconds(timeoutSeconds), RandomString = TestBase.GetRandomString(20), ReturnHost = "https://localhost", Sender = "localhost", SessionId = Guid.NewGuid() }));
        var page = TestBase.CurrentPage as NotificationVerifyRequestView;
        Assert.AreEqual(page.Account.Guid, accountGuid);
        Assert.AreEqual(page.ReturnHost, "https://localhost");
        Assert.AreEqual(page.RequesterName, "localhost");
        return page;
    }

    public static ConfirmByPinView ChooseColorWithPin(NotificationVerifyRequestView notificationPage, Xamarin.Forms.Color color)
    {
        if (notificationPage.Color1 == color)
            notificationPage.ImageButton1_OnClicked(new Button(), null);
        if (notificationPage.Color2 == color)
            notificationPage.ImageButton2_OnClicked(new Button(), null);
        if (notificationPage.Color3 == color)
            notificationPage.ImageButton3_OnClicked(new Button(), null);

        while (!(TestBase.CurrentPage is ConfirmByPinView))
        {
            Thread.Sleep(1);
        }

        var confirmByPin = TestBase.CurrentPage as ConfirmByPinView;
        return confirmByPin;
    }

    public static MainView ChooseColorWithoutPin(NotificationVerifyRequestView notificationPage, Xamarin.Forms.Color color)
    {
        TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();

        if (notificationPage.Color1 == color)
            notificationPage.ImageButton1_OnClicked(new Button(), null);
        if (notificationPage.Color2 == color)
            notificationPage.ImageButton2_OnClicked(new Button(), null);
        if (notificationPage.Color3 == color)
            notificationPage.ImageButton3_OnClicked(new Button(), null);

        while (!(TestBase.CurrentPage is MainView))
        {
            Thread.Sleep(1);
        }

        var mainPage = TestBase.CurrentPage as MainView;
        if (mainPage._loadAccountTask != null)
            while (!mainPage._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
        return mainPage;

    }
}