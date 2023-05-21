using System.Threading;
using AndroidTests.Tools;
using AppCommon;
using AppConfig;
using NUnit.Framework;
using passi_android;
using passi_android.Notifications;
using passi_android.Tools;

namespace AndroidTests.TestClasses;

public class ConfirmByPinClass
{
    public static MainView ConfirmByPin(ConfirmByPinView confirmByPinView)
    {
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        confirmByPinView.NumbersPad_OnNumberClicked("1");
        TestRestService.Result[ConfigSettings.Authorize] = TestBase.SuccesfullResponce();

        confirmByPinView.NumbersPad_OnNumberClicked("1");

        while (!(TestBase.CurrentPage is MainView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentPage is MainView);
        var mainPage = TestBase.CurrentPage as MainView;
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
        App.FingerPrintReadingResult.Invoke(new FingerPrintResult());
        Assert.IsTrue(TestBase.CurrentPage is LoadingView);

        while (!(TestBase.CurrentPage is MainView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentPage is MainView);
        var mainPage = TestBase.CurrentPage as MainView;
        if (mainPage._loadAccountTask != null)
            while (!mainPage._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
        return mainPage;

    }
}