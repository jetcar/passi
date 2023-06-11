using System.Threading;
using AndroidTests.Tools;
using AppCommon;
using AppConfig;
using NUnit.Framework;
using passi_android;
using passi_android.Main;
using passi_android.Notifications;
using passi_android.Tools;

namespace AndroidTests.TestClasses;

public class ConfirmByPinTestClass
{
    public static MainView ConfirmByPin(ConfirmByPinView confirmByPinView)
    {
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("2");
        confirmByPinView.NumbersPad_OnNumberClicked("del");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();

        confirmByPinView.NumbersPad_OnNumberClicked("1");

        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is MainView);
        var mainPage = TestBase.CurrentView as MainView;
        if (mainPage._loadAccountTask != null)
            while (!mainPage._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
        return mainPage;
    }

    public static MainView ConfirmByFingerprint(ConfirmByPinView confirmByPinView)
    {
        TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();
        TestBase.TouchFingerPrintWithGoodResult();
        Assert.IsTrue(TestBase.CurrentView is LoadingView);

        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is MainView);
        var mainPage = TestBase.CurrentView as MainView;
        if (mainPage._loadAccountTask != null)
            while (!mainPage._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
        return mainPage;

    }

    public static ConfirmByPinView ConfirmByIncorrectPin(ConfirmByPinView confirmByPinView)
    {
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();

        confirmByPinView.NumbersPad_OnNumberClicked("2");

        while (!(TestBase.CurrentView is ConfirmByPinView) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is ConfirmByPinView);
        var pinView = TestBase.CurrentView as ConfirmByPinView;
        
        return pinView;
    }

    public static ConfirmByPinView ConfirmByPinBadResponse(ConfirmByPinView confirmByPinView)
    {
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        TestRestService.Result[ConfigSettings.Authorize] = TestBase.BadResponce("error");

        confirmByPinView.NumbersPad_OnNumberClicked("1");

        while (!(TestBase.CurrentView is ConfirmByPinView) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is ConfirmByPinView);
        var pinView = TestBase.CurrentView as ConfirmByPinView;

        return pinView;
    }

    public static ConfirmByPinView ConfirmByPinNetworkError(ConfirmByPinView confirmByPinView)
    {
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        TestRestService.Result[ConfigSettings.Authorize] = TestBase.FailedResponce();

        confirmByPinView.NumbersPad_OnNumberClicked("1");

        while (!(TestBase.CurrentView is ConfirmByPinView) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is ConfirmByPinView);
        var pinView = TestBase.CurrentView as ConfirmByPinView;

        return pinView;
    }
}