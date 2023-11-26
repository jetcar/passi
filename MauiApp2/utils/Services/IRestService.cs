using MauiApp2.StorageModels;
using RestSharp;

namespace MauiApp2.utils.Services
{
    public interface IRestService
    {
        Task<RestResponse> ExecuteGetAsync(ProviderDb provider, string requestUri);
        Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri);
        Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri, Method method);
        Task<RestResponse> ExecutePostAsync<T>(ProviderDb provider, string requestUri, T item) where T : class;
    }
}