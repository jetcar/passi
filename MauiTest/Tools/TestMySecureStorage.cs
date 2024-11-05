using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MauiCommonServices;
using MauiViewModels.utils.Services;
using Newtonsoft.Json;

namespace MauiTest.Tools;

internal class TestMySecureStorage : IMySecureStorage
{
    public Dictionary<string, string> _dict = new Dictionary<string, string>();

    public Task<T> GetAsync<T>(string keyName)
    {
        Console.WriteLine("get from storage " + keyName);
        if (!_dict.ContainsKey(keyName))
            _dict[keyName] = "";
        return Task.FromResult(JsonConvert.DeserializeObject<T>(_dict[keyName]) ?? default);
    }

    public Task<string> GetAsync(string keyName)
    {
        Console.WriteLine("get from storage " + keyName);
        if (!_dict.ContainsKey(keyName))
            _dict[keyName] = "";

        return Task.FromResult(_dict[keyName]);
    }

    public Task SetAsync<T>(string keyName, T item) where T : class
    {
        Console.WriteLine("save to storage " + keyName);
        _dict[keyName] = JsonConvert.SerializeObject(item);
        return Task.FromResult(_dict[keyName]);
    }

    public Task SetAsync(string keyName, string item)
    {
        Console.WriteLine("save to storage " + keyName);
        _dict[keyName] = item;
        return Task.FromResult(_dict[keyName]);
    }

    public Task Remove(string keyName)
    {
        Console.WriteLine("remove from storage " + keyName);
        return Task.FromResult(_dict.Remove(keyName));
    }
}