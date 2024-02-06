using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using MauiViewModels.StorageModels;
using MauiViewModels.utils.Services;
using RestSharp;

namespace MauiTest.Tools;

internal class TestRestService : IRestService
{
    public Task<RestResponse> ExecuteGetAsync(ProviderDb provider, string requestUri)
    {
        Console.WriteLine("request started:" + requestUri);
        RestResponse result = null;
        if (Result.ContainsKey(requestUri))
        {
            result = Result[requestUri];
        }
        Result.Remove(requestUri);

        return Task.FromResult(result ?? new RestResponse());
    }

    public Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri)
    {
        Console.WriteLine("request started:" + requestUri);
        RestResponse result = null;
        if (Result.ContainsKey(requestUri))
        {
            result = Result[requestUri];
        }
        Result.Remove(requestUri);

        return Task.FromResult(result ?? new RestResponse());
    }

    public Task<RestResponse> ExecuteAsync(ProviderDb provider, string requestUri, Method method)
    {
        Console.WriteLine("request started:" + requestUri);
        RestResponse result = null;
        if (Result.ContainsKey(requestUri))
        {
            result = Result[requestUri];
        }
        Result.Remove(requestUri);

        return Task.FromResult(result ?? new RestResponse());
    }

    public Task<RestResponse> ExecutePostAsync<T>(ProviderDb provider, string requestUri, T item) where T : class
    {
        var validationContext = new ValidationContext(item);
        Validator.ValidateObject(item, validationContext);
        Console.WriteLine("request started:" + requestUri);
        RestResponse result = null;
        if (Result.ContainsKey(requestUri))
        {
            result = Result[requestUri];
        }
        Result.Remove(requestUri);

        return Task.FromResult(result ?? new RestResponse());
    }

    public static Dictionary<string, RestResponse> Result = new Dictionary<string, RestResponse>();
}