using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ConfigurationManager;
using Google.Cloud.Diagnostics.Common;
using Newtonsoft.Json;
using Serilog;
using Polly;
using Polly.Retry;
using PostSharp.Extensibility;
using Repos;
using RestSharp;

namespace Services;

[Profile(AttributeTargetElements = MulticastTargets.Method)]
public class MyRestClient : IMyRestClient
{
    private static RestClient _client;
    private AppSetting _appSetting;
    private IManagedTracer _tracer;
    private readonly ILogger _logger;

    private static int _maxRetryAttempts = 10;
    private static TimeSpan _pauseBetweenFailures = TimeSpan.FromSeconds(1);
    private readonly AsyncRetryPolicy<RestResponse> _retryPolicy;

    public MyRestClient(AppSetting appSetting, IManagedTracer tracer, ILogger logger)
    {
        _retryPolicy = Policy
            .HandleResult<RestResponse>(x =>
            {
                return !x.IsSuccessful && x.StatusCode != HttpStatusCode.BadRequest;
            })
            .WaitAndRetryAsync(_maxRetryAttempts, x => _pauseBetweenFailures, (iRestResponse, timeSpan, retryCount, context) =>
            {
                _logger.Warning($"The request failed. HttpStatusCode={iRestResponse.Result.StatusCode}. Waiting {timeSpan} seconds before retry. Number attempt {retryCount}. Uri={iRestResponse.Result.ResponseUri}; RequestResponse={iRestResponse.Result.Content}");
            });

        var passiUrl = Environment.GetEnvironmentVariable("PassiUrl") ?? _appSetting["PassiUrl"];

        _appSetting = appSetting;
        _tracer = tracer;
        _logger = logger;
        var options = new RestClientOptions(passiUrl);
        if (Debugger.IsAttached)
            options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        if (_client == null)
            _client = new RestClient(options);
    }

    public Task<RestResponse> ExecuteAsync(RestRequest request)
    {
        var currentTraceId = _tracer.GetCurrentTraceId();
        request.AddHeader("custom_trace_id", currentTraceId);
        _logger.Debug($"{_client.Options.BaseUrl} {request.Resource}: {JsonConvert.SerializeObject(request.Parameters)}");
        return _retryPolicy.ExecuteAsync(
            () => _client.ExecuteAsync(request));
    }
}