using System.Threading;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Main;
using NUnit.Framework;
using AccountViewModel = MauiViewModels.Main.AccountViewModel;

namespace MauiTest.TestClasses;

public class MainTestClass
{
    public static MainView OpenMainPage()
    {
        TestBase.Navigation.PushModalSinglePage(new MainView());
        var page = TestBase.CurrentView as MainView;

        if (page._loadAccountTask != null)
            while (!page._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }

        return page;
    }

    public static TermsAgreementsViewModel ClickAddAccount(MainView view)
    {
        view.Button_AddAccount();
        Assert.IsTrue(TestBase.CurrentView is TermsAgreementsViewModel);

        var tcView = TestBase.CurrentView as TermsAgreementsViewModel;
        return tcView;
    }

    public static AccountViewModel OpenAccount(MainView mainView, MauiViewModels.ViewModels.AccountModel account)
    {
        mainView.Cell_OnTapped(account);

        while (!(TestBase.CurrentView is AccountViewModel))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is AccountViewModel);

        var accountView = TestBase.CurrentView as AccountViewModel;
        return accountView;
    }
}