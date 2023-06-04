using System.Threading;
using AndroidTests.Tools;
using AppCommon;
using NUnit.Framework;
using passi_android;
using passi_android.FingerPrint;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class FingerPrintTestClass
{
    public static FingerPrintConfirmByPinView AddFingerPrint(AccountView accountView)
    {
        accountView.AddBiometric_Button_OnClicked(new Button(), null);

        TestBase.TouchFingerPrintWithGoodResult();

        while (!(TestBase.CurrentPage is FingerPrintConfirmByPinView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentPage is FingerPrintConfirmByPinView);


        var fingerPrintConfirmByPinView = TestBase.CurrentPage as FingerPrintConfirmByPinView;
        return fingerPrintConfirmByPinView;
    }
    public static void AddFingerPrintNoPin(AccountView accountView)
    {
        accountView.AddBiometric_Button_OnClicked(new Button(), null);
        TestBase.TouchFingerPrintWithGoodResult();


        while (!(TestBase.CurrentPage is MainView))
        {
            Thread.Sleep(1);
        }
        var mainpage = TestBase.CurrentPage as MainView;
        while (!mainpage._loadAccountTask.IsCompleted)
        {
            Thread.Sleep(1);
        }
        Assert.IsTrue(TestBase.CurrentPage is MainView);
    }
}