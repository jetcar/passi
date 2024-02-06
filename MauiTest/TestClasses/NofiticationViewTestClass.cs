using System;
using System.Collections.Generic;
using System.Threading;
using AppConfig;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Notifications;
using MauiViewModels.utils.Services;
using MauiViewModels.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using WebApiDto;
using WebApiDto.Auth;
using Color = WebApiDto.Auth.Color;

namespace MauiTest.TestClasses;

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

        while (!(TestBase.CurrentView is NotificationVerifyRequestView))
        {
            Thread.Sleep(1);
        }
        var page = TestBase.CurrentView as NotificationVerifyRequestView;
        while (page._account == null)
            Thread.Sleep(1);
        Assert.AreEqual(page._account.Guid, accountViewModel.Guid);
        Assert.AreEqual(page.ReturnHost, "https://localhost");
        Assert.AreEqual(page.RequesterName, "localhost");
        return page;
    }

    public static MainView PollExistingSessionId(AccountViewModel accountViewModel, Color color, int timeoutSeconds, Guid? sessionid = null)
    {
        // TestNavigationService.navigationsCount = 0;
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
        // Assert.AreEqual(1, TestNavigationService.navigationsCount);
        var page = TestBase.CurrentView as MainView;
        return page;
    }

    public static ConfirmByPinView ChooseColorWithPin(NotificationVerifyRequestView notificationPage, Microsoft.Maui.Graphics.Color color)
    {
        TestNavigationService.navigationsCount = 0;
        if (notificationPage.Color1 == color)
            notificationPage.ImageButton1_OnClicked();
        if (notificationPage.Color2 == color)
            notificationPage.ImageButton2_OnClicked();
        if (notificationPage.Color3 == color)
            notificationPage.ImageButton3_OnClicked();

        while (!(TestBase.CurrentView is ConfirmByPinView))
        {
            Thread.Sleep(1);
        }
        Assert.AreEqual(1, TestNavigationService.navigationsCount);

        var confirmByPin = TestBase.CurrentView as ConfirmByPinView;
        return confirmByPin;
    }

    public static MainView ChooseColorWithoutPin(NotificationVerifyRequestView notificationPage, Microsoft.Maui.Graphics.Color color)
    {
        TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();

        if (notificationPage.Color1 == color)
            notificationPage.ImageButton1_OnClicked();
        if (notificationPage.Color2 == color)
            notificationPage.ImageButton2_OnClicked();
        if (notificationPage.Color3 == color)
            notificationPage.ImageButton3_OnClicked();

        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }

        var mainPage = TestBase.CurrentView as MainView;
        if (mainPage._loadAccountTask != null)
            while (!mainPage._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
        return mainPage;
    }
}