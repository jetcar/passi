using System.Threading;
using AndroidTests.Tools;
using AppConfig;
using NUnit.Framework;
using passi_android;
using passi_android.Registration;
using passi_android.Tools;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class FinishConfirmationTestClass
{
    public static MainView FinishRegistrationWithPin(FinishConfirmationView finishConfirmationView)
    {
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("confirm");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        TestRestService.Result[ConfigSettings.SignupConfirmation] = TestBase.SuccesfullResponce();

        finishConfirmationView.NumbersPad_OnNumberClicked("confirm");
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

    public static MainView FinishRegistrationSkipPin(FinishConfirmationView finishConfirmationView)
    {
        TestRestService.Result[ConfigSettings.SignupConfirmation] = TestBase.SuccesfullResponce();
        finishConfirmationView.SkipButton_OnClicked(new Button(), null);
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