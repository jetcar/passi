using passi_android.StorageModels;
using RestSharp;
using System.Threading.Tasks;

namespace passi_android.utils.Services
{
    public interface IRestService
    {
        Task<RestResponse> ExecuteGetAsync(ProviderDb provider, string requestUri);

        Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri);

        Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri, Method method);

        Task<RestResponse> ExecutePostAsync<T>(ProviderDb provider, string requestUri, T item) where T : class;
    }
}