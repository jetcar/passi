using System.Collections.Generic;
using NUnit.Framework;
using OpenIDC.Helpers;
using OpenIDC.Models;

namespace OpenIDCTests
{
    public class RedirectUriHelperTests
    {
        private static OidcClient Web(params string[] uris) => new() { RedirectUris = new List<string>(uris), IsPublicClient = false };
        private static OidcClient App(params string[] uris) => new() { RedirectUris = new List<string>(uris), IsPublicClient = true };

        [Test]
        public void ExactMatchIsAccepted()
        {
            Assert.That(RedirectUriHelper.IsAllowed("https://site/cb", Web("https://site/cb")), Is.True);
        }

        [TestCase("http://site/cb")]
        [TestCase("https://other/cb")]
        [TestCase("https://site:8443/cb")]
        [TestCase("https://site/CB")]
        [TestCase("")]
        [TestCase(null)]
        public void MismatchIsRejected(string redirectUri)
        {
            Assert.That(RedirectUriHelper.IsAllowed(redirectUri, Web("https://site/cb")), Is.False);
        }

        [TestCase("http://localhost:53682/callback", "http://localhost/callback")]
        [TestCase("http://localhost:53682/callback", "http://localhost:8080/callback")]
        [TestCase("http://127.0.0.1:9999/callback", "http://127.0.0.1/callback")]
        [TestCase("http://[::1]:9999/callback", "http://[::1]/callback")]
        public void PublicClientLoopbackRedirectMayUseAnyPort(string redirectUri, string registered)
        {
            // RFC 8252 section 7.3: native apps (e.g. MCP clients like Claude Code) listen on an ephemeral port.
            Assert.That(RedirectUriHelper.IsAllowed(redirectUri, App(registered)), Is.True);
        }

        [Test]
        public void LoopbackPortFlexibilityDoesNotApplyToConfidentialClients()
        {
            Assert.That(RedirectUriHelper.IsAllowed("http://localhost:53682/callback", Web("http://localhost:8080/callback")), Is.False);
        }

        [TestCase("http://localhost:53682/other", "http://localhost/callback")]
        [TestCase("http://127.0.0.1:53682/callback", "http://localhost/callback")]
        [TestCase("https://localhost:53682/callback", "http://localhost/callback")]
        [TestCase("http://evil.localhost:53682/callback", "http://localhost/callback")]
        public void LoopbackStillRequiresSameSchemeHostAndPath(string redirectUri, string registered)
        {
            Assert.That(RedirectUriHelper.IsAllowed(redirectUri, App(registered)), Is.False);
        }

        [Test]
        public void AuthorizationResponseEncodesState()
        {
            Assert.That(RedirectUriHelper.BuildAuthorizationResponse("https://site/cb", "abc", "x y&z=1"),
                Is.EqualTo("https://site/cb?code=abc&state=x%20y%26z%3D1"));
        }

        [Test]
        public void AuthorizationResponseKeepsExistingQueryAndOmitsMissingState()
        {
            Assert.That(RedirectUriHelper.BuildAuthorizationResponse("https://site/cb?tenant=1", "abc", null),
                Is.EqualTo("https://site/cb?tenant=1&code=abc"));
        }
    }
}
