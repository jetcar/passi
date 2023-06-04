using System;
using System.Collections.Generic;
using System.Threading;
using AndroidTests.Tools;
using AppConfig;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using passi_android;
using passi_android.Notifications;
using passi_android.Tools;
using passi_android.utils.Services;
using passi_android.ViewModels;
using RestSharp;
using WebApiDto;
using WebApiDto.Auth;
using Xamarin.Forms;
using Color = WebApiDto.Auth.Color;

namespace AndroidTests.TestClasses;

public class NofiticationViewTestClass
{
    public static NotificationVerifyRequestView PollOpenSessions(AccountViewModel accountViewModel, Color color, int timeoutSeconds, Guid? sessionid = null)
    {
        TestRestService.Result[ConfigSettings.SyncAccounts] = TestBase.SuccesfullResponce<List<AccountMinDto>>(new List<AccountMinDto>()
        {
            new AccountMinDto()
            {
                UserGuid = accountViewModel.Guid,
                Username = accountViewModel.Email
            }
        });

        TestRestService.Result[ConfigSettings.CheckForStartedSessions] = TestBase.SuccesfullResponce<NotificationDto>(new NotificationDto()
        {
            AccountGuid = accountViewModel.Guid,
            ConfirmationColor = color,
            ExpirationTime = DateTime.UtcNow.AddSeconds(timeoutSeconds),
            RandomString = TestBase.GetRandomString(20),
            ReturnHost = "https://localhost",
            Sender = "localhost",
            SessionId = sessionid ?? Guid.NewGuid(),

        });
        var syncService = App.Services.GetService<ISyncService>();
        syncService.PollNotifications();
        while (!(syncService.PollingTask.IsCompleted))
            Thread.Sleep(1);

        while (!(TestBase.CurrentPage is NotificationVerifyRequestView))
        {
            Thread.Sleep(1);
        }
        var page = TestBase.CurrentPage as NotificationVerifyRequestView;
        while (page._account == null)
            Thread.Sleep(1);
        Assert.AreEqual(page._account.Guid, accountViewModel.Guid);
        Assert.AreEqual(page.ReturnHost, "https://localhost");
        Assert.AreEqual(page.RequesterName, "localhost");
        return page;
    }

    public static MainView PollExistingSessionId(AccountViewModel accountViewModel, Color color, int timeoutSeconds, Guid? sessionid = null)
    {
        TestRestService.Result[ConfigSettings.SyncAccounts] = TestBase.SuccesfullResponce<List<AccountMinDto>>(new List<AccountMinDto>()
        {
            new AccountMinDto()
            {
                UserGuid = accountViewModel.Guid,
                Username = accountViewModel.Email
            }
        });

        TestRestService.Result[ConfigSettings.CheckForStartedSessions] = TestBase.SuccesfullResponce<NotificationDto>(new NotificationDto()
        {
            AccountGuid = accountViewModel.Guid,
            ConfirmationColor = color,
            ExpirationTime = DateTime.UtcNow.AddSeconds(timeoutSeconds),
            RandomString = TestBase.GetRandomString(20),
            ReturnHost = "https://localhost",
            Sender = "localhost",
            SessionId = sessionid ?? Guid.NewGuid(),

        });
        var syncService = App.Services.GetService<ISyncService>();
        syncService.PollNotifications();
        while (!(syncService.PollingTask.IsCompleted))
        {
            Thread.Sleep(1);
        }
        var page = TestBase.CurrentPage as MainView;
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