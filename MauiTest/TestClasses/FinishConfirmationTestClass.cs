using System.Threading;
using AppConfig;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Registration;
using MauiViewModels.Tools;
using NUnit.Framework;

namespace MauiTest.TestClasses;

public class FinishConfirmationTestClass
{
    public static MainView FinishRegistrationWithPin(FinishConfirmationView finishConfirmationView)
    {
        TestNavigationService.navigationsCount = 0;
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.ClearPin1_OnClicked();
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("2");
        finishConfirmationView.NumbersPad_OnNumberClicked("del");//test editing
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("confirm");
        Assert.AreEqual(4, finishConfirmationView.Pin1.Length);
        Assert.AreEqual("****", finishConfirmationView.Pin1Masked);

        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.ClearPin2_OnClicked();
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("2");
        finishConfirmationView.NumbersPad_OnNumberClicked("del");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        finishConfirmationView.NumbersPad_OnNumberClicked("1");
        Assert.AreEqual(4, finishConfirmationView.Pin2.Length);
        Assert.AreEqual("****", finishConfirmationView.Pin2Masked);
        //TestRestService.Result[ConfigSettings.SignupConfirmation] = TestBase.SuccesfullResponce();

        finishConfirmationView.NumbersPad_OnNumberClicked("confirm");
        Assert.IsTrue(TestBase.CurrentView is LoadingView);

        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is MainView);
        Assert.AreEqual(2, TestNavigationService.navigationsCount);
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
        TestNavigationService.navigationsCount = 0;
        //TestRestService.Result[ConfigSettings.SignupConfirmation] = TestBase.SuccesfullResponce();
        finishConfirmationView.SkipButton_OnClicked();
        Assert.IsTrue(TestBase.CurrentView is LoadingView);

        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }
        Assert.AreEqual(2, TestNavigationService.navigationsCount);

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