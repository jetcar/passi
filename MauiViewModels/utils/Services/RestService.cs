﻿using System;
using System.Threading.Tasks;
using MauiViewModels.StorageModels;
using Newtonsoft.Json;
using RestSharp;

namespace MauiViewModels.utils.Services;

public class RestService : IRestService
{
    public Task<RestResponse> ExecuteGetAsync(ProviderDb provider, string requestUri)
    {
        var client = GetClient(provider);
        var request = new RestRequest(requestUri, Method.Get);
        request.Timeout = TimeSpan.FromSeconds(100);
        var message = $"{client.Options.BaseUrl} {request.Resource}: {JsonConvert.SerializeObject(request.Parameters)}";
        System.Diagnostics.Debug.WriteLine(message);
        return client.ExecuteAsync(request);
    }

    public Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri)
    {
        var client = GetClient(provider);
        var request = new RestRequest(requestUri, Method.Get);
        request.Timeout = TimeSpan.FromSeconds(100);
        var message = $"{client.Options.BaseUrl} {request.Resource}: {JsonConvert.SerializeObject(request.Parameters)}";
        System.Diagnostics.Debug.WriteLine(message);
        return client.ExecuteAsync(request);
    }

    public Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri, Method method)
    {
        var client = GetClient(provider);
        var request = new RestRequest(requestUri, method);
        request.Timeout = TimeSpan.FromSeconds(100);
        var message = $"{client.Options.BaseUrl} {request.Resource}: {JsonConvert.SerializeObject(request.Parameters)}";
        System.Diagnostics.Debug.WriteLine(message);
        return client.ExecuteAsync(request);
    }

    public Task<RestResponse> ExecutePostAsync<T>(ProviderDb provider, string requestUri, T item) where T : class
    {
        var client = GetClient(provider);
        var request = new RestRequest(requestUri, Method.Post);
        request.Timeout = TimeSpan.FromSeconds(30);

        request.AddJsonBody(item);
        var message = $"{client.Options.BaseUrl} {request.Resource}: {JsonConvert.SerializeObject(request.Parameters)}";
        System.Diagnostics.Debug.WriteLine(message);
        return client.ExecuteAsync(request);
    }

    private RestClient GetClient(ProviderDb provider)
    {
        var client = new RestClient(provider.PassiWebApiUrl);
        return client;
    }
}