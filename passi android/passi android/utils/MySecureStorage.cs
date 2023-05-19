using System.Threading.Tasks;
using Newtonsoft.Json;
using Xamarin.Essentials;

namespace passi_android.utils
{
    public class MySecureStorage : IMySecureStorage
    {
        public async Task<T> GetAsync<T>(string keyName)
        {
            var json = SecureStorage.GetAsync(keyName).Result ?? "";
            return JsonConvert.DeserializeObject<T>(json) ?? default(T);
        }

        public async Task<string> GetAsync(string keyName)
        {
            var json = SecureStorage.GetAsync(keyName).Result ?? "";
            return json;
        }

        public async Task SetAsync<T>(string keyName, T item) where T : class
        {
            await SecureStorage.SetAsync(keyName, JsonConvert.SerializeObject(item));
        }

        public async Task SetAsync(string keyName, string item)
        {
            await SecureStorage.SetAsync(keyName, item);
        }

        public async Task Remove(string keyName)
        {
            SecureStorage.Remove(keyName);
        }
    }
}