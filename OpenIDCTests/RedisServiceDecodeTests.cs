using Newtonsoft.Json;
using JsonSerializer = ServiceStack.Text.JsonSerializer;
using NUnit.Framework;
using OpenIDC.Models;
using RedisClient;
using ServiceStack.Text;

namespace OpenIDCTests
{
    public class RedisServiceDecodeTests
    {
        [Test]
        public void RawValueReadInsideTransactionDecodesBackToTheStoredObject()
        {
            var authCode = new AuthorizationCode { Code = "abc", ClientId = "SampleApp", Subject = "alice" };
            var json = JsonConvert.SerializeObject(authCode);

            // RedisService.Add stores via IRedisClient.Set<string>, which ServiceStack JSON-encodes
            // (ToJson) into a quoted string literal; a GET queued in a MULTI/EXEC transaction returns
            // those raw bytes without undoing that encoding.
            var rawStoredValue = JsonSerializer.SerializeToString(json);

            var decoded = RedisService.DecodeStoredString(rawStoredValue);

            var result = JsonConvert.DeserializeObject<AuthorizationCode>(decoded);
            Assert.That(result.Code, Is.EqualTo("abc"));
            Assert.That(result.ClientId, Is.EqualTo("SampleApp"));
            Assert.That(result.Subject, Is.EqualTo("alice"));
        }
    }
}
