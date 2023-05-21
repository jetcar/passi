using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using passi_android.utils.Services;
using Xamarin.Forms;

namespace AndroidTests.Tools;

internal class TestNavigationService : INavigationService
{
    private List<Page> _pages = new List<Page>();
    public void PushModalSinglePage(Page page)
    {
        _pages.Add(page);
        if(TestBase.CurrentPage != null)
            TestBase.CurrentPage.SendDisappearing();
        TestBase.CurrentPage = page;
        page.SendAppearing();
    }

    public void NavigateTop()
    {
        var page = _pages[0];
        for (int i = _pages.Count - 1; i > 0; i--)
        {
            _pages.RemoveAt(i);
        }
        if(TestBase.CurrentPage != null)
            TestBase.CurrentPage.SendDisappearing();

        TestBase.CurrentPage = page;
        page.SendAppearing();

    }

    public Task PopModal()
    {
        _pages.RemoveAt(_pages.Count - 1);
        var page = _pages.Last();
        if(TestBase.CurrentPage != null)
            TestBase.CurrentPage.SendDisappearing();

        TestBase.CurrentPage = page;
        page.SendAppearing();
        return Task.CompletedTask;
    }
}