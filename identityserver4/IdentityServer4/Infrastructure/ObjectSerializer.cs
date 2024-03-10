using Newtonsoft.Json;

namespace IdentityServer4.Infrastructure
{
    [GoogleTracer.Profile]
    public static class ObjectSerializer
    {
        public static string ToString(object o)
        {
            return JsonConvert.SerializeObject(o);
        }

        public static T FromString<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value);
        }
    }
}