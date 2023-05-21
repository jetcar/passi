using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace passi_android.utils.Services
{
    public interface INavigationService
    {
        void PushModalSinglePage(Page page);
        void NavigateTop();
        Task PopModal();
    }

}