using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiViewModels;
using MauiViewModels.utils.Services;

namespace MauiTest.Tools;

internal class TestNavigationService : INavigationService
{
    public static int navigationsCount = 0;
    private List<BaseViewModel> _pages = new List<BaseViewModel>();
    public static string AlertMessage { get; set; }

    public async Task PushModalSinglePage(BaseViewModel viewModel)
    {
        _pages.Add(viewModel);
        Console.WriteLine(viewModel.ToString());
        if (TestBase.CurrentView != null)
        {
            TestBase.CurrentView.OnDisappearing(null, null);
        }
        TestBase.CurrentView = viewModel;
        Interlocked.Increment(ref navigationsCount);
        viewModel.OnAppearing(null, null);
    }

    public async Task NavigateTop()
    {
        var page = _pages[0];
        var pagesCount = _pages.Count;
        for (int i = pagesCount - 1; i > 0; i--)
        {
            _pages.RemoveAt(i);
        }
        if (TestBase.CurrentView != null)
            TestBase.CurrentView.OnDisappearing(null, null);

        TestBase.CurrentView = page;
        Console.WriteLine(page.ToString());
        Interlocked.Increment(ref navigationsCount);
        page.OnAppearing(null, null);
    }

    public async Task PopModal()
    {
        _pages.RemoveAt(_pages.Count - 1);
        var page = _pages.Last();
        if (TestBase.CurrentView != null)
        {
            var currentView = TestBase.CurrentView;
            Task.Run(() =>
            {
                currentView.OnDisappearing(null, null);
            });
        }

        TestBase.CurrentView = page;
        Console.WriteLine(page.ToString());
        Interlocked.Increment(ref navigationsCount);
        page.OnAppearing(null, null);
    }

    public void DisplayAlert(string header, string content, string okText)
    {
        AlertMessage = content;
    }
}