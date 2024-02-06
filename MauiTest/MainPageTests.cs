using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using AppConfig;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.ViewModels;
using NUnit.Framework;
using WebApiDto.Auth;

namespace MauiTest
{
    public class MainPageTests : TestBase
    {
        [Test]
        public void OpenMainView()
        {
            CreateAccount();

            var page = new MainView();
            page.OnAppearing();
            var accounts = new ObservableCollection<AccountViewModel>();
            SecureRepository.LoadAccountIntoList(accounts);
            TestRestService.Result[ConfigSettings.SyncAccounts] = TestBase.SuccesfullResponce<List<AccountMinDto>>(new List<AccountMinDto>()
            {
                new AccountMinDto()
                {
                    UserGuid = accounts[0].Guid,
                    Username = accounts[0].Email
                }
            });
            while (!page._loadAccountTask.IsCompleted)
            {
                Thread.Sleep(1);
            }

            Assert.AreEqual(page.Accounts.Count, 1);
        }

        [Test]
        public void OpenMainViewSyncButton()
        {
            CreateAccount();

            var page = new MainView();
            page.OnAppearing();
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            page.Button_Sync();
            Assert.AreEqual(page.Accounts.Count, 1);
        }

        [Test, Timeout(10000)]
        public void DeleteAccountMainView()
        {
            CreateAccount();

            var page = new MainView();
            page.OnAppearing();
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
            Assert.AreEqual(page.Accounts.Count, 0);
        }
    }
}