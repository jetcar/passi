
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using AndroidTests.Tools;
using AppConfig;
using NUnit.Framework;
using passi_android;
using passi_android.Main;
using passi_android.ViewModels;
using WebApiDto.Auth;
using Xamarin.Forms;

namespace AndroidTests
{
    public class MainPageTests : TestBase
    {


        [Test]
        public void OpenMainView()
        {
            CreateAccount();

            var page = new MainView();
            page.SendAppearing();
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
            page.SendAppearing();
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            var button = new Button();
            page.Button_Sync(button, null);
            Assert.AreEqual(button.IsEnabled, true);
            Assert.AreEqual(page.Accounts.Count, 1);
        }

        [Test, Timeout(10000)]
        public void DeleteAccountMainView()
        {
            CreateAccount();

            var page = new MainView();
            page.SendAppearing();
            while (page.Accounts.Count == 0)
            {
                Thread.Sleep(1);
            }
            page.Button_ShowDeleteAccount(null, null);
            var button = new Button();
            button.BindingContext = page.Accounts[0];
            page.Button_DeleteAccount(button, null);

            while (page.Accounts.Count != 0)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(page.Accounts.Count, 0);
        }
    }
}