using passi_android.utils;
using Xamarin.Forms;

namespace AndroidTests.Tools;

internal class TestNavigationService : INavigationService
{
    public void PushModalSinglePage(Page page)
    {
        TestBase.CurrentPage = page;
        page.SendAppearing();
    }
}