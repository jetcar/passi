using RestSharp;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Services
{
    public interface IMyRestClient
    {
        Task<RestResponse> ExecuteAsync(RestRequest request);
    }

    public class MyRestClient : IMyRestClient
    {
        private readonly RestClient _client;

        public MyRestClient(string baseUrl)
        {
            var options = new RestClientOptions(baseUrl);
            if (Debugger.IsAttached)
                options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            _client = new RestClient(options);
        }

        public Task<RestResponse> ExecuteAsync(RestRequest request)
        {
            return _client.ExecuteAsync(request);
        }
    }
}
