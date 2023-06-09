using System.Threading;
using AndroidTests.Tools;
using AppConfig;
using NUnit.Framework;
using passi_android;
using passi_android.Main;
using passi_android.Registration;
using passi_android.Tools;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class FinishConfirmationTestClass
{
    public static MainView FinishRegistrationWithPin(FinishConfirmationView finishConfirmationView)
    {
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.ClearPin1_OnClicked(new Button(),null);
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("2");
        finishConfirmationView.NumbersPad_OnNumberClicked("del");//test editing
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("confirm");
        Assert.AreEqual(4,finishConfirmationView.Pin1.Length);
        Assert.AreEqual("****",finishConfirmationView.Pin1Masked);

        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.ClearPin2_OnClicked(new Button(), null);
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("2");
        finishConfirmationView.NumbersPad_OnNumberClicked("del");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        Assert.AreEqual(4,finishConfirmationView.Pin2.Length);
        Assert.AreEqual("****",finishConfirmationView.Pin2Masked);
        TestRestService.Result[ConfigSettings.SignupConfirmation] = TestBase.SuccesfullResponce();

        finishConfirmationView.NumbersPad_OnNumberClicked("confirm");
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

    public static MainView FinishRegistrationSkipPin(FinishConfirmationView finishConfirmationView)
    {
        TestRestService.Result[ConfigSettings.SignupConfirmation] = TestBase.SuccesfullResponce();
        finishConfirmationView.SkipButton_OnClicked(new Button(), null);
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
}