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
    public static MainView FinishRegistrationWithPin(FinishConfirmationViewModel finishConfirmationViewModel)
    {
        TestNavigationService.navigationsCount = 0;
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.ClearPin1_OnClicked();
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("2");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("del");//test editing
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("confirm");
        Assert.AreEqual(4, finishConfirmationViewModel.Pin1.Length);
        Assert.AreEqual("****", finishConfirmationViewModel.Pin1Masked);

        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.ClearPin2_OnClicked();
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("2");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("del");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        Assert.AreEqual(4, finishConfirmationViewModel.Pin2.Length);
        Assert.AreEqual("****", finishConfirmationViewModel.Pin2Masked);
        //TestRestService.Result[ConfigSettings.SignupConfirmation] = TestBase.SuccesfullResponce();

        finishConfirmationViewModel.NumbersPad_OnNumberClicked("confirm");
        Assert.IsTrue(TestBase.CurrentView is LoadingViewModel);

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

    public static MainView FinishRegistrationSkipPin(FinishConfirmationViewModel finishConfirmationViewModel)
    {
        TestNavigationService.navigationsCount = 0;
        //TestRestService.Result[ConfigSettings.SignupConfirmation] = TestBase.SuccesfullResponce();
        finishConfirmationViewModel.SkipButton_OnClicked();
        Assert.IsTrue(TestBase.CurrentView is LoadingViewModel);

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