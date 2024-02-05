using System.Threading.Tasks;

namespace passi_android.utils.Services
{
    public interface INavigationService
    {
        Task PushModalSinglePage(BaseContentPage page);

        Task NavigateTop();

        Task PopModal();
    }
}