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
using Microsoft.Maui.Graphics;
using Newtonsoft.Json;
using NUnit.Framework;
using RestSharp;
using WebApiDto;
using WebApiDto.Auth;
using static System.Net.WebRequestMethods;
using Color = WebApiDto.Auth.Color;

namespace MauiTest.TestClasses;

public class NofiticationViewTestClass
{
    public static NotificationVerifyRequestView PollOpenSessions(AccountViewModel accountViewModel, out Microsoft.Maui.Graphics.Color color)
    {
        var rest = new RestSharp.RestClient(ConfigSettings.IdentityUrl);
        var restRequest = new RestRequest(ConfigSettings.IdentityLogin, Method.Post);
        var obj = new
        {
            rememberLogin = true,
            Username = accountViewModel.Email,
            ReturnUrl = $"{ConfigSettings.IdentityUrl}/connect/authorize?client_id=SampleApp&redirect_uri=https%3A%2F%2Flocalhost%2Foauth%2Fcallback&response_type=code&scope=openid%20email&code_challenge=naq4WgJouw-4gyRmlWCsYMjynH7aT6On92H5H6OxC2w&code_challenge_method=S256&response_mode=form_post&nonce=638430118036268745.ZWQ2MDVmNjgtNGQ3NC00ZmVmLWE1YTktNDEyMmVlOGFkNGYzYThjYzBmZjUtZDQzMC00NzNhLTg3NmQtMDgyODkwOGQ2NzI5&audience=SampleApp&state=CfDJ8HFKsKgPAtZJgnT-WDK-c52mXgamSGksMvXSiemVP5XqU3n9VbAmv6bclGtzDdnxqOnvWu9rJwsVn0Z78DUxyyXEHEEMFL4KLGXkeUx8dyCVWkAIxPiRnKyxu485cDKqlCYBCaHiH7d1K08g42IQmT-QFf9jRp9rY3or4Z-bKhF2SjInYEi-tAMf8sFXJS11pm_biKa5Wi3DI2JcipYVFJA6TIVjwcHmYERd0pUYkA2xi-eOBCm4ZC4X_5nF_BS_BCOigWkDEYzK0161izJldxU-qHp8M8dnujrUkGhnH6UjMb1Lg-tT7tlWeU7klFynZVcl2n8NuunTzzOynHTWfNJ2EHfmxmoF8M9SgLJmvXyY0hb28_w7_pQGSqykFg3yEQ&x-client-SKU=ID_NET8_0&x-client-ver=7.2.0.0"
        };
        restRequest.AddJsonBody(obj);
        var result = rest.ExecuteAsync(restRequest).Result;
        Assert.IsTrue(result.IsSuccessful);
        Console.WriteLine($"{result.StatusCode} {result.Content}");
        var json = JsonConvert.DeserializeObject<dynamic>(result.Content);
        var username = json.username.ToString();
        var checkColor = json.checkColor.ToString();
        Microsoft.Maui.Graphics.Color.TryParse(checkColor, out color);

        Assert.AreEqual(accountViewModel.Email, username);
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
        Assert.AreEqual(page.ReturnHost, "localhost");
        Assert.AreEqual(page.RequesterName, "IdentityServer");
        return page;
    }

    public static MainView PollExistingSessionId(AccountViewModel accountViewModel, Color color, int timeoutSeconds, Guid? sessionid = null)
    {
        // TestNavigationService.navigationsCount = 0;
        //TestRestService.Result[ConfigSettings.SyncAccounts] = TestBase.SuccesfullResponce<List<AccountMinDto>>(new List<AccountMinDto>()
        //{
        //    new AccountMinDto()
        //    {
        //        UserGuid = accountViewModel.Guid,
        //        Username = accountViewModel.Email
        //    }
        //});

        //TestRestService.Result[ConfigSettings.CheckForStartedSessions] = TestBase.SuccesfullResponce<NotificationDto>(new NotificationDto()
        //{
        //    AccountGuid = accountViewModel.Guid,
        //    ConfirmationColor = color,
        //    ExpirationTime = DateTime.UtcNow.AddSeconds(timeoutSeconds),
        //    RandomString = TestBase.GetRandomString(20),
        //    ReturnHost = "https://localhost",
        //    Sender = "localhost",
        //    SessionId = sessionid ?? Guid.NewGuid(),
        //});
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
        //TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();

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

    public static NotificationVerifyRequestView ChooseColorWithoutPinFingerPrint(NotificationVerifyRequestView notificationPage, Microsoft.Maui.Graphics.Color color)
    {
        //TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();

        if (notificationPage.Color1 == color)
            notificationPage.ImageButton1_OnClicked();
        if (notificationPage.Color2 == color)
            notificationPage.ImageButton2_OnClicked();
        if (notificationPage.Color3 == color)
            notificationPage.ImageButton3_OnClicked();

        while (!(TestBase.CurrentView is NotificationVerifyRequestView))
        {
            Thread.Sleep(1);
        }

        var mainPage = TestBase.CurrentView as NotificationVerifyRequestView;

        return mainPage;
    }
}