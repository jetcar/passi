using System.Collections.ObjectModel;
using System.Threading;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.ViewModels;
using NUnit.Framework;

namespace MauiTest
{
    public class MainPageTests : TestBase
    {
        [Test]
        public void OpenMainView()
        {
            CreateAccount();
            CreateAccount();
            CreateAccount();

            var page = new MainView();
            page.OnAppearing(null, null);
            var accounts = new ObservableCollection<AccountModel>();
            SecureRepository.LoadAccountIntoList(accounts);

            while (!page._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }
            page.Button_ShowDeleteAccount();
            page.Button_PreDeleteAccount(page.Accounts[0]);
            Assert.That(true, Is.EqualTo(page.Accounts[0].IsDeleteVisible));
            Assert.That(page.Accounts.Count, Is.EqualTo(3));
            Thread.Sleep(2000);
            Assert.That(true, Is.EqualTo(page.Accounts[0].IsDeleteVisible));
        }

        [Test]
        public void OpenMainViewSyncButton()
        {
            CreateAccount();

            var page = new MainView();
            page.OnAppearing(null, null);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            page.Button_Sync();
            Assert.That(page.Accounts.Count, Is.EqualTo(1));
        }

        [Test, MaxTime(30000)]
        public void DeleteAccountMainView()
        {
            CreateAccount();

            var page = new MainView();
            page.OnAppearing(null, null);
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            page.Button_ShowDeleteAccount();
            page.Button_DeleteAccount(page.Accounts[0]);

            while (page.Accounts.Count != 0)
            {
                Thread.Sleep(1);
            }
            Assert.That(page.Accounts.Count, Is.EqualTo(0));
        }
    }
}