using System.Collections.Generic;
using Xamarin.Forms;

namespace passi_android.utils
{
    public interface INavigationService
    {
        void PushModalSinglePage(Page page);
        void NavigateTop();
        void PopModal();
    }

    public class NavigationService : INavigationService
    {
        private List<Page> pages = new List<Page>();
        public void PushModalSinglePage(Page page)
        {
            pages.Add(page);
            App.FirstPage.Navigation.PushModalSinglePage(page);
        }

        public void NavigateTop()
        {
            var page = pages[0];
            for (int i = pages.Count - 1; i > 0; i--)
            {
                pages.RemoveAt(i);
            }
            App.FirstPage.Navigation.PushModalSinglePage(page);

        }

        public void PopModal()
        {
            App.FirstPage.Navigation.PopModal();
        }
    }
}