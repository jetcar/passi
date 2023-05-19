using System.Threading.Tasks;

namespace passi_android.utils
{
    public interface IMySecureStorage
    {
        Task<T> GetAsync<T>(string keyName);
        Task<string> GetAsync(string keyName);
        Task SetAsync<T>(string keyName, T item) where T : class;
        Task SetAsync(string keyName, string item);
        Task Remove(string keyName);
    }
}