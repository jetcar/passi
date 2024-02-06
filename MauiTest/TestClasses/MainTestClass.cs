using System.Threading;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Main;
using MauiViewModels.ViewModels;
using NUnit.Framework;

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

    public static TermsAgreementsView ClickAddAccount(MainView view)
    {
        view.Button_AddAccount();
        Assert.IsTrue(TestBase.CurrentView is TermsAgreementsView);

        var tcView = TestBase.CurrentView as TermsAgreementsView;
        return tcView;
    }

    public static AccountView OpenAccount(MainView mainView, AccountViewModel account)
    {
        mainView.Cell_OnTapped(account);

        while (!(TestBase.CurrentView is AccountView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is AccountView);

        var accountView = TestBase.CurrentView as AccountView;
        return accountView;
    }
}