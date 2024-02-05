using AndroidTests.Tools;
using NUnit.Framework;
using passi_android.FingerPrint;
using passi_android.Main;
using passi_android.Tools;
using System.Threading;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class FingerPrintTestClass
{
    public static FingerPrintConfirmByPinView AddFingerPrint(AccountView accountView)
    {
        accountView.AddBiometric_Button_OnClicked(new Button(), null);

        TestBase.TouchFingerPrintWithGoodResult();

        while (!(TestBase.CurrentView is FingerPrintConfirmByPinView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is FingerPrintConfirmByPinView);

        var fingerPrintConfirmByPinView = TestBase.CurrentView as FingerPrintConfirmByPinView;
        return fingerPrintConfirmByPinView;
    }

    public static void AddFingerPrintNoPin(AccountView accountView)
    {
        TestNavigationService.navigationsCount = 0;
        accountView.AddBiometric_Button_OnClicked(new Button(), null);
        TestBase.TouchFingerPrintWithGoodResult();
        Assert.IsTrue(TestBase.CurrentView is LoadingView);

        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }
        Assert.AreEqual(2, TestNavigationService.navigationsCount);
        var mainpage = TestBase.CurrentView as MainView;
        while (!mainpage._loadAccountTask.IsCompleted)
        {
            Thread.Sleep(1);
        }
        Assert.IsTrue(TestBase.CurrentView is MainView);
    }
}