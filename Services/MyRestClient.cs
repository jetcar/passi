using ConfigurationManager;
using Google.Cloud.Diagnostics.Common;
using Newtonsoft.Json;

using RestSharp;
using Serilog;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using GoogleTracer;

namespace Services;

[Profile]
public class MyRestClient : IMyRestClient
{
    private static RestClient _client;
    private AppSetting _appSetting;
    private IManagedTracer _tracer;
    private readonly ILogger _logger;

    public MyRestClient(AppSetting appSetting, IManagedTracer tracer, ILogger logger)
    {
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
        var currentTraceId = _tracer.GetCurrentTraceId() ?? Guid.NewGuid().ToString();
        request.AddHeader("custom_trace_id", currentTraceId);
        request.Timeout = 10000;
        _logger.Debug($"{_client.Options.BaseUrl} {request.Resource}: {JsonConvert.SerializeObject(request.Parameters)}");
        return _client.ExecuteWithRetryAsync(request);
    }
}

public static class RestClientExtensions
{
    // Extension method to retry a request
    public static async Task<RestResponse> ExecuteWithRetryAsync(this RestClient client, RestRequest request, int maxAttempts = 10, int delayBetweenAttemptsInSeconds = 1)
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
            else
            {
                // Log or handle non-success status codes as needed
                Console.WriteLine($"Attempt {attempt} failed: {response.StatusCode}");
            }

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