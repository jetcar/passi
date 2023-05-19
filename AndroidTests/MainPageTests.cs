
using System.Threading;
using AndroidTests.Tools;
using NUnit.Framework;
using passi_android;
using Xamarin.Forms;

namespace AndroidTests
{
    public class MainPageTests : TestBase
    {


        [Test]
        public void OpenMainView()
        {
            CreateAccount();

            var page = new MainPage();
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

        [Test,Timeout(10000)]
        public void DeleteAccountMainView()
        {
            CreateAccount();

            var page = new MainPage();
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