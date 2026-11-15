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
            Assert.That(RedirectUriHelper.IsAllowed("https://site/cb?tenant=1", Web("https://site/cb?tenant=1")), Is.True);
        }

        [TestCase("http://site/cb")]
        [TestCase("https://other/cb")]
        [TestCase("https://site:8443/cb")]
        [TestCase("https://site/CB")]
        [TestCase("https://site/cb")]
        [TestCase("https://site/cb?tenant=2")]
        [TestCase("https://site/cb?tenant=1&next=evil")]
        [TestCase("https://user@site/cb?tenant=1")]
        [TestCase("https://site/cb?tenant=1#fragment")]
        [TestCase("")]
        [TestCase(null)]
        public void MismatchIsRejected(string redirectUri)
        {
            Assert.That(RedirectUriHelper.IsAllowed(redirectUri, Web("https://site/cb?tenant=1")), Is.False);
        }

        [TestCase("http://localhost:53682/callback", "http://localhost/callback")]
        [TestCase("http://localhost:53682/callback", "http://localhost:8080/callback")]
        [TestCase("http://127.0.0.1:9999/callback", "http://127.0.0.1/callback")]
        [TestCase("http://[::1]:9999/callback", "http://[::1]/callback")]
        [TestCase("http://localhost:53682/callback?tenant=1", "http://localhost/callback?tenant=1")]
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
        [TestCase("http://localhost:53682/callback?tenant=2", "http://localhost/callback?tenant=1")]
        [TestCase("http://user@localhost:53682/callback", "http://localhost/callback")]
        [TestCase("http://localhost:53682/callback#fragment", "http://localhost/callback")]
        public void LoopbackStillRequiresSameSchemeHostAndPath(string redirectUri, string registered)
        {
            Assert.That(RedirectUriHelper.IsAllowed(redirectUri, App(registered)), Is.False);
        }

        [Test]
        public void DynamicClientStillRequiresExactRegisteredRedirect()
        {
            var client = App("https://site/cb?tenant=1");
            client.IsDynamic = true;

            Assert.That(RedirectUriHelper.IsAllowed("https://site/cb?tenant=1", client), Is.True);
            Assert.That(RedirectUriHelper.IsAllowed("https://site/cb?tenant=2", client), Is.False);
            Assert.That(RedirectUriHelper.IsAllowed("https://site/cb?tenant=1#fragment", client), Is.False);
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

        [Test]
        public void AuthorizationErrorResponseEncodesDescriptionAndState()
        {
            Assert.That(RedirectUriHelper.BuildAuthorizationErrorResponse(
                    "https://site/cb", "invalid_scope", "Unsupported scope: admin", "x y&z=1"),
                Is.EqualTo("https://site/cb?error=invalid_scope&error_description=Unsupported%20scope%3A%20admin&state=x%20y%26z%3D1"));
        }

        [Test]
        public void AuthorizationErrorResponseKeepsExistingQueryAndOmitsOptionalValues()
        {
            Assert.That(RedirectUriHelper.BuildAuthorizationErrorResponse(
                    "https://site/cb?tenant=1", "invalid_request", null, null),
                Is.EqualTo("https://site/cb?tenant=1&error=invalid_request"));
        }
    }
}
