using System.Threading;
using AndroidTests.Tools;
using AppCommon;
using NUnit.Framework;
using passi_android;
using passi_android.FingerPrint;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class FingerPrintClass
{
    public static FingerPrintConfirmByPinView AddFingerPrint(AccountView accountView)
    {
        TestBase.EnableFingerPrintWithGoodResult();
        accountView.AddBiometric_Button_OnClicked(new Button(), null);


        while (!(TestBase.CurrentPage is FingerPrintConfirmByPinView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentPage is FingerPrintConfirmByPinView);


        var fingerPrintConfirmByPinView = TestBase.CurrentPage as FingerPrintConfirmByPinView;
        return fingerPrintConfirmByPinView;
    }
}