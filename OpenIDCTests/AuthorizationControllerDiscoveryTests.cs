using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ConfigurationManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using OpenIDC.Controllers;

namespace OpenIDCTests
{
    public class AuthorizationControllerDiscoveryTests
    {
        private static JsonElement Discovery()
        {
            var appSetting = new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["AppSetting:IdentityUrlBase"] = "https://passi.cloud" })
                .Build()) { PrefferAppsettingFile = true };
            var controller = new AuthorizationController(null, null, null, null, appSetting);

            var value = ((OkObjectResult)controller.Discovery()).Value;
            return JsonDocument.Parse(JsonSerializer.Serialize(value)).RootElement;
        }

        [Test]
        public void AdvertisesPublicClientsWithoutSecret()
        {
            // MCP clients (and other native apps) authenticate with PKCE only and look for "none" here.
            var methods = Discovery().GetProperty("token_endpoint_auth_methods_supported").EnumerateArray().Select(e => e.GetString());

            Assert.That(methods, Does.Contain("none"));
        }

        [Test]
        public void AdvertisesS256Pkce()
        {
            var methods = Discovery().GetProperty("code_challenge_methods_supported").EnumerateArray().Select(e => e.GetString());

            Assert.That(methods, Does.Contain("S256"));
        }
    }
}
