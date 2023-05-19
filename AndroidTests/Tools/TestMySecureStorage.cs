using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using passi_android.utils;

namespace AndroidTests.Tools;

internal class TestMySecureStorage : IMySecureStorage
{
    public Dictionary<string, string> _dict = new Dictionary<string, string>();
    public Task<T> GetAsync<T>(string keyName)
    {
        if (!_dict.ContainsKey(keyName))
            _dict[keyName] = "";
        return Task.FromResult(JsonConvert.DeserializeObject<T>(_dict[keyName]) ?? Activator.CreateInstance<T>());
    }

    public Task<string> GetAsync(string keyName)
    {
        if (!_dict.ContainsKey(keyName))
            _dict[keyName] = "";

        return Task.FromResult(_dict[keyName]);
    }

    public Task SetAsync<T>(string keyName, T item) where T : class
    {
        _dict[keyName] = JsonConvert.SerializeObject(item);
        return Task.FromResult(_dict[keyName]);
    }

    public Task SetAsync(string keyName, string item)
    {
        _dict[keyName] = item;
        return Task.FromResult(_dict[keyName]);
    }

    public Task Remove(string keyName)
    {
        return Task.FromResult(_dict.Remove(keyName));
    }
}