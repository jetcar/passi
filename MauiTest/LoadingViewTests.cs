using System.Threading;
using System.Threading.Tasks;
using MauiTest.Tools;
using MauiViewModels;
using MauiViewModels.Tools;
using MauiViewModels.utils.Services;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace MauiTest
{
    public class LoadingViewTests : TestBase
    {
        [Test, Timeout(100000)]
        public void LoadingViewExpired()
        {
            App.SkipLoadingTimer = false;
            TestBase.Navigation.PushModalSinglePage(new MainView());
            TestBase.Navigation.PushModalSinglePage(new LoadingView(() =>
            {
                Task.Delay(2000);
            }, 1000));

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is LoadingView))
            {
                Thread.Sleep(1);
            }

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is MainView))
            {
                Thread.Sleep(1);
            }
        }

        [Test, Timeout(100000)]
        public void LoadingViewNavigateBack()
        {
            TestBase.Navigation.PushModalSinglePage(new MainView());
            TestBase.Navigation.PushModalSinglePage(new LoadingView(() =>
            {
                App.Services.GetService<INavigationService>().PopModal();
            }));

            while (!TestBase.CurrentView.Appeared || !(TestBase.CurrentView is MainView))
            {
                Thread.Sleep(1);
            }
        }
    }
}