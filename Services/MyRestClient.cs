using ConfigurationManager;
using Newtonsoft.Json;
using RestSharp;
using log4net;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Google.Cloud.Diagnostics.Common;
using GoogleTracer;

namespace Services;

[Profile]
public class MyRestClient : IMyRestClient
{
    internal const int DefaultTimeoutSeconds = 10;
    internal const int DefaultMaxRetryAttempts = 10;
    internal const int DefaultRetryDelaySeconds = 1;

    private static RestClient _client;
    private AppSetting _appSetting;
    private IManagedTracer _tracer;
    private static readonly ILog _logger = LogManager.GetLogger(typeof(MyRestClient));

    public MyRestClient(AppSetting appSetting, IManagedTracer tracer)
    {
        _appSetting = appSetting;
        var passiUrl = Environment.GetEnvironmentVariable("PassiUrl") ?? _appSetting["PassiUrl"];
        _tracer = tracer;
        var options = new RestClientOptions(passiUrl);
        if (Debugger.IsAttached)
            options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        if (_client == null)
            _client = new RestClient(options);
    }

    public Task<RestResponse> ExecuteAsync(RestRequest request)
    {
        var currentTraceId = _tracer.GetCurrentTraceId() ?? Guid.NewGuid().ToString();
        request.AddHeader("custom_trace_id", currentTraceId);
        request.Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds);
        _logger.Debug($"{_client.Options.BaseUrl} {request.Resource}: {JsonConvert.SerializeObject(request.Parameters)}");
        return _client.ExecuteWithRetryAsync(request);
    }
}

public static class RestClientExtensions
{
    // Extension method to retry a request
    public static async Task<RestResponse> ExecuteWithRetryAsync(this RestClient client, RestRequest request, int maxAttempts = MyRestClient.DefaultMaxRetryAttempts, int delayBetweenAttemptsInSeconds = MyRestClient.DefaultRetryDelaySeconds)
    {
        if (client == null)
            throw new ArgumentNullException(nameof(client));
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts should be at least 1");
        if (delayBetweenAttemptsInSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(delayBetweenAttemptsInSeconds), "Delay between attempts should be non-negative");

        RestResponse response = null;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest)
            {
                // Success, no need to retry
                return response;
            }
            // Note: Logging handled by caller

            if (attempt < maxAttempts)
            {
                // Wait before retrying
                await Task.Delay(TimeSpan.FromSeconds(delayBetweenAttemptsInSeconds));
            }
        }

        // Return the last response received (success or failure)
        return response;
    }
}