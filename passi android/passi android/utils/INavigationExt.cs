using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace passi_android.utils
{
    public static class INavigationExt
    {
        public static void NavigateTop(this INavigation navigator)
        {
            var modalStackCount = navigator.ModalStack.Count;
            for (int i = 0; i < modalStackCount; i++)
            {
                navigator.PopModal();
            }
        }

        public static Task PopModal(this INavigation navigator)
        {
            return navigator.PopModalAsync();
        }

        public static void PushModalSinglePage(this INavigation navigator, Page page)
        {
            var modalStackCount = navigator.ModalStack.ToList();
            var lastOrDefault = modalStackCount.LastOrDefault();
            if (lastOrDefault != null && lastOrDefault.GetType() == page.GetType())
                return;
            navigator.PushModalAsync(page);
        }
    }
}