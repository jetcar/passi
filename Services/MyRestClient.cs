using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ConfigurationManager;
using Google.Cloud.Diagnostics.Common;
using Microsoft.AspNetCore.Http;
using PostSharp.Extensibility;
using RestSharp;

namespace Services;

[Profile(AttributeTargetElements = MulticastTargets.Method)]
public class MyRestClient : IMyRestClient
{
    private readonly RestClient _client;
    private AppSetting _appSetting;
    private IManagedTracer _tracer;
    public MyRestClient(AppSetting appSetting, IManagedTracer tracer)
    {
        var passiUrl = Environment.GetEnvironmentVariable("PassiUrl") ?? _appSetting["PassiUrl"];

        _appSetting = appSetting;
        _tracer = tracer;
        var options = new RestClientOptions(passiUrl);
        if (Debugger.IsAttached)
            options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        _client = new RestClient(options);
    }

    public Task<RestResponse> ExecuteAsync(RestRequest request)
    {
        var currentTraceId = _tracer.GetCurrentTraceId();
        request.AddHeader("custom_trace_id", currentTraceId);
        return _client.ExecuteAsync(request);
    }
}