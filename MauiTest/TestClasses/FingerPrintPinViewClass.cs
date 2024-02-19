using System.Threading;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.FingerPrint;
using MauiViewModels.Tools;
using NUnit.Framework;

namespace MauiTest.TestClasses;

public class FingerPrintPinViewClass
{
    public static void FinishFingerPrintAdding(FingerPrintConfirmByPinViewModel fingerPrintConfirmByPinView)
    {
        TestNavigationService.navigationsCount = 0;
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
        Assert.AreEqual(2, TestNavigationService.navigationsCount);
        var mainpage = TestBase.CurrentView as MainView;
        while (!mainpage._loadAccountTask.IsCompleted)
        {
            Thread.Sleep(1);
        }
        Assert.IsTrue(TestBase.CurrentView is MainView);
    }

    public static FingerPrintConfirmByPinViewModel FinishFingerPrintAddingIncorrectPin(FingerPrintConfirmByPinViewModel fingerPrintConfirmByPinView)
    {
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("del");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("2");
        fingerPrintConfirmByPinView.NumbersPad_OnNumberClicked("1");
        Assert.IsTrue(TestBase.CurrentView is LoadingViewModel);
        Assert.AreEqual("****", fingerPrintConfirmByPinView.Pin1Masked);
        while (!(TestBase.CurrentView is FingerPrintConfirmByPinViewModel) || !TestBase.CurrentView.Appeared)
        {
            Thread.Sleep(1);
        }

        return TestBase.CurrentView as FingerPrintConfirmByPinViewModel;
    }
}