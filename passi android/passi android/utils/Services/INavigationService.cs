using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace passi_android.utils.Services
{
    public interface INavigationService
    {
        Task PushModalSinglePage(Page page);
        Task NavigateTop();
        Task PopModal();
    }

}