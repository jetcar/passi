using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace passi_android.utils.Services
{
    public class NavigationService : INavigationService
    {
        private List<Page> _pages = new List<Page>();
        public void PushModalSinglePage(Page page)
        {
            _pages.Add(page);
            App.FirstPage.Navigation.PushModalSinglePage(page);
        }

        public void NavigateTop()
        {
            for (int i = _pages.Count - 1; i > 0; i--)
            {
                _pages.RemoveAt(i);
            }
            App.FirstPage.Navigation.NavigateTop();

        }

        public Task PopModal()
        {
            _pages.RemoveAt(_pages.Count - 1);
            return App.FirstPage.Navigation.PopModal();
        }
    }
}