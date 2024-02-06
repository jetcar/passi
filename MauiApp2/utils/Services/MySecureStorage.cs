using MauiViewModels.utils.Services;
using Newtonsoft.Json;

namespace MauiApp2.utils.Services
{
    public class MySecureStorage : IMySecureStorage
    {
        public async Task<T> GetAsync<T>(string keyName)
        {
            var json = SecureStorage.GetAsync(keyName).Result ?? "";
            return JsonConvert.DeserializeObject<T>(json) ?? default;
        }

        public async Task<string> GetAsync(string keyName)
        {
            var json = SecureStorage.GetAsync(keyName).Result ?? "";
            return json;
        }

        public async Task SetAsync<T>(string keyName, T item) where T : class
        {
            var serializeObject = JsonConvert.SerializeObject(item);
            Task.Run(async () =>
            {
                await SecureStorage.SetAsync(keyName, serializeObject);
            });
        }

        public async Task SetAsync(string keyName, string item)
        {
            Task.Run(async () =>
            {
                await SecureStorage.SetAsync(keyName, item);
            });
        }

        public async Task Remove(string keyName)
        {
            SecureStorage.Remove(keyName);
        }
    }
}