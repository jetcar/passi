using System.Threading;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.FingerPrint;
using MauiViewModels.Main;
using MauiViewModels.Tools;
using NUnit.Framework;

namespace MauiTest.TestClasses;

public class FingerPrintTestClass
{
    public static FingerPrintConfirmByPinViewModel AddFingerPrint(AccountViewModel accountView)
    {
        accountView.AddBiometric_Button_OnClicked();

        TestBase.TouchFingerPrintWithGoodResult();

        while (!(TestBase.CurrentView is FingerPrintConfirmByPinViewModel))
        {
            Thread.Sleep(1);
        }

        Assert.That(TestBase.CurrentView is FingerPrintConfirmByPinViewModel);

        var fingerPrintConfirmByPinView = TestBase.CurrentView as FingerPrintConfirmByPinViewModel;
        return fingerPrintConfirmByPinView;
    }

    public static void AddFingerPrintNoPin(AccountViewModel accountView)
    {
        TestNavigationService.navigationsCount = 0;
        accountView.AddBiometric_Button_OnClicked();
        TestBase.TouchFingerPrintWithGoodResult();
        Assert.That(TestBase.CurrentView is LoadingViewModel);

        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }
        Assert.That(2, Is.EqualTo(TestNavigationService.navigationsCount));
        var mainpage = TestBase.CurrentView as MainView;
        while (!mainpage._loadAccountTask.IsCompleted)
        {
            Thread.Sleep(1);
        }
        Assert.That(TestBase.CurrentView is MainView);
    }
}