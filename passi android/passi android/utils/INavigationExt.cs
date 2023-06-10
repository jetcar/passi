using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace passi_android.utils
{
    public static class INavigationExt
    {
        public static async Task NavigateTop(this INavigation navigator)
        {
           // var modalStackCount = navigator.ModalStack.Count;
            navigator.PopToRootAsync();
           //// for (int i = 0; i < modalStackCount; i++)
           // {
           //     await navigator.PopModal();
           // }
        }

        public static async Task PopModal(this INavigation navigator)
        {
            await navigator.PopModalAsync();
        }

        public static async Task PushModalSinglePage(this INavigation navigator, Page page)
        {
            var modalStackCount = navigator.ModalStack.ToList();
            var lastOrDefault = modalStackCount.LastOrDefault();
            if (lastOrDefault != null && lastOrDefault.GetType() == page.GetType())
                return;
            await navigator.PushModalAsync(page);
        }
    }
}