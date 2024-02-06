using System.Threading.Tasks;

namespace MauiViewModels.utils.Services;

public interface INavigationService
{
    Task PushModalSinglePage(BaseContentPage page);

    Task NavigateTop();

    Task PopModal();

    void DisplayAlert(string header, string content, string okText);
}