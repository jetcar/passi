using RestSharp;
using System.Threading.Tasks;

namespace Services
{
    public interface IMyRestClient
    {
        Task<RestResponse> ExecuteAsync(RestRequest request);
    }
}
