using System.Threading;
using AndroidTests.Tools;
using NUnit.Framework;
using passi_android;
using passi_android.FingerPrint;
using passi_android.Main;
using passi_android.Tools;

namespace AndroidTests.TestClasses;

public class FingerPrintPinViewClass
{
    public static void FinishFingerPrintAdding(FingerPrintConfirmByPinView fingerPrintConfirmByPinView)
    {
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("del");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        Assert.AreEqual("****", fingerPrintConfirmByPinView.Pin1Masked);
        while (!(TestBase.CurrentView is MainView))
        {
            Thread.Sleep(1);
        }
        var mainpage = TestBase.CurrentView as MainView;
        while (!mainpage._loadAccountTask.IsCompleted)
        {
            Thread.Sleep(1);
        }
        Assert.IsTrue(TestBase.CurrentView is MainView);
    }
    public static FingerPrintConfirmByPinView FinishFingerPrintAddingIncorrectPin(FingerPrintConfirmByPinView fingerPrintConfirmByPinView)
    {
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("del");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("2");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        Assert.IsTrue(TestBase.CurrentView is LoadingView);
        Assert.AreEqual("****", fingerPrintConfirmByPinView.Pin1Masked);
        while (!(TestBase.CurrentView is FingerPrintConfirmByPinView) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        return TestBase.CurrentView as FingerPrintConfirmByPinView;
    }
}