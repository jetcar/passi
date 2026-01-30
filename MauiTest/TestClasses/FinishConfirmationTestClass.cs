using System.Threading;
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
        Assert.That(4,Is.EqualTo(finishConfirmationViewModel.Pin1.Length));
        Assert.That("****", Is.EqualTo(finishConfirmationViewModel.Pin1Masked));

        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.ClearPin2_OnClicked();
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("2");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("del");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        finishConfirmationViewModel.NumbersPad_OnNumberClicked("1");
        Assert.That(4, Is.EqualTo(finishConfirmationViewModel.Pin2.Length));
        Assert.That("****", Is.EqualTo(finishConfirmationViewModel.Pin2Masked));
        //TestRestService.Result[ConfigSettings.SignupConfirmation] = TestBase.SuccesfullResponce();

        finishConfirmationViewModel.NumbersPad_OnNumberClicked("confirm");
        Assert.That(TestBase.CurrentView is LoadingViewModel);

        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }

        Assert.That(TestBase.CurrentView is MainView);
        Assert.That(2, Is.EqualTo(TestNavigationService.navigationsCount));
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
}