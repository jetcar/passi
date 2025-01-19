using System;
using System.Threading;
using AppConfig;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Notifications;
using MauiViewModels.Tools;
using NUnit.Framework;

namespace MauiTest.TestClasses;

public class ConfirmByPinTestClass
{
    public static MainView ConfirmByPin(ConfirmByPinViewModel confirmByPinView)
    {
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("2");
        confirmByPinView.NumbersPad_OnNumberClicked("del");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        //TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();
        TestNavigationService.navigationsCount = 0;
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        Assert.That(TestBase.CurrentView is LoadingViewModel);

        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }
        Assert.That(2, Is.EqualTo(TestNavigationService.navigationsCount));

        Assert.That(TestBase.CurrentView is MainView);
        var mainPage = TestBase.CurrentView as MainView;
        if (mainPage._loadAccountTask != null)
            while (!mainPage._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
        return mainPage;
    }

    public static MainView ConfirmByFingerprint(ConfirmByPinViewModel confirmByPinView)
    {
        TestNavigationService.navigationsCount = 0;
        //TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();
        TestBase.TouchFingerPrintWithGoodResult();
        Assert.That(TestBase.CurrentView is LoadingViewModel);

        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }
        Assert.That(2, Is.EqualTo(TestNavigationService.navigationsCount));

        Assert.That(TestBase.CurrentView is MainView);
        var mainPage = TestBase.CurrentView as MainView;
        if (mainPage._loadAccountTask != null)
            while (!mainPage._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
        return mainPage;
    }

    public static ConfirmByPinViewModel ConfirmByIncorrectPin(ConfirmByPinViewModel confirmByPinView)
    {
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        //TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();

        confirmByPinView.NumbersPad_OnNumberClicked("2");

        while (!(TestBase.CurrentView is ConfirmByPinViewModel) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.That(TestBase.CurrentView is ConfirmByPinViewModel);
        var pinView = TestBase.CurrentView as ConfirmByPinViewModel;

        return pinView;
    }

    public static ConfirmByPinViewModel ConfirmByPinBadResponse(ConfirmByPinViewModel confirmByPinView)
    {
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        //TestRestService.Result[ConfigSettings.Authorize] = TestBase.BadResponce("error");
        confirmByPinView.Message.SessionId =Guid.Empty;

        confirmByPinView.NumbersPad_OnNumberClicked("1");
        while (!(TestBase.CurrentView is ConfirmByPinViewModel) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.That(TestBase.CurrentView is ConfirmByPinViewModel);
        var pinView = TestBase.CurrentView as ConfirmByPinViewModel;

        return pinView;
    }

    public static ConfirmByPinViewModel ConfirmByPinNetworkError(ConfirmByPinViewModel confirmByPinView)
    {
        ConfigSettings.PassiUrl = "http://localhost:9999";
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        //TestRestService.Result[ConfigSettings.Authorize] = TestBase.FailedResponce();

        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView._accountDb.Provider.PassiWebApiUrl = "http://localhost:9999";
        while (!(TestBase.CurrentView is ConfirmByPinViewModel) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.That(TestBase.CurrentView is ConfirmByPinViewModel);
        var pinView = TestBase.CurrentView as ConfirmByPinViewModel;

        return pinView;
    }
}