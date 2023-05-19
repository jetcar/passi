using System.Threading;
using AndroidTests.Tools;
using NUnit.Framework;
using passi_android;
using Xamarin.Forms;

namespace AndroidTests.FunctionalTests
{
    public class RegistrationTests : TestBase
    {


        [Test]
        public void OpenMainView()
        {
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

            page.Button_AddAccount(null,null);
            Assert.IsTrue(CurrentPage is AddAccountPage);
        }

        
    }
}