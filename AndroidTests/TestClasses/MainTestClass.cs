using System.Threading;
using AndroidTests.Tools;
using NUnit.Framework;
using passi_android;
using passi_android.Main;
using passi_android.ViewModels;
using Xamarin.Forms;

namespace AndroidTests.TestClasses;

public class MainTestClass
{
    public static MainView OpenMainPage()
    {
        TestBase.Navigation.PushModalSinglePage(new MainView());
        var page = TestBase.CurrentView as MainView;
        if (App.FirstPage == null)
            App.FirstPage = page;
        if (page._loadAccountTask != null)
            while (!page._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }

        return page;
    }
    public static TermsAgreementsView ClickAddAccount(MainView view)
    {
        view.Button_AddAccount(null, null);
        Assert.IsTrue(TestBase.CurrentView is TermsAgreementsView);

        var tcView = TestBase.CurrentView as TermsAgreementsView;
        return tcView;
    }

    public static AccountView OpenAccount(MainView mainView, AccountViewModel account)
    {
        mainView.Cell_OnTapped(new ViewCell() { BindingContext = account }, null);

        while (!(TestBase.CurrentView is AccountView))
        {
            Thread.Sleep(1);
        }

        Assert.IsTrue(TestBase.CurrentView is AccountView);

        var accountView = TestBase.CurrentView as AccountView;
        return accountView;
    }
}