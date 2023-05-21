using System.Threading;
using AndroidTests.Tools;
using NUnit.Framework;
using passi_android;
using passi_android.FingerPrint;

namespace AndroidTests.TestClasses;

public class FingerPrintPinViewClass
{
    public static void FinishFingerPrintAdding(FingerPrintConfirmByPinView fingerPrintConfirmByPinView)
    {
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");

        while (!(TestBase.CurrentPage is MainView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentPage is MainView);
    }
}