
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using AndroidTests.Tools;
using AppConfig;
using NUnit.Framework;
using passi_android;
using passi_android.Main;
using passi_android.Tools;
using passi_android.ViewModels;
using WebApiDto.Auth;
using Xamarin.Forms;

namespace AndroidTests
{
    public class LoadingViewTests : TestBase
    {


        [Test, Timeout(100000)]
        public void LoadingViewExpired()
        {
            TestBase.Navigation.PushModalSinglePage(new MainView());
            TestBase.Navigation.PushModalSinglePage(new LoadingView(() =>
            {
                Task.Delay(40000);
            }));

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is LoadingView))
            {
                Thread.Sleep(1);
            }

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is MainView))
            {
                Thread.Sleep(1);
            }

        }
       
    }
}