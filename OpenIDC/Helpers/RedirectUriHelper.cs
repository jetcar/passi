using System;
using OpenIDC.Models;

namespace OpenIDC.Helpers
{
    public static class RedirectUriHelper
    {
        /// <summary>
        /// Redirect URIs must match a registered URI exactly (scheme, host, port, case-sensitive path). The one
        /// exception is RFC 8252 section 7.3: for public clients, an http loopback URI may use any port, because native
        /// apps and CLI agents listen on an ephemeral port.
        /// </summary>
        public static bool IsAllowed(string redirectUri, OidcClient client)
        {
            if (string.IsNullOrEmpty(redirectUri) || client?.RedirectUris == null || !Uri.TryCreate(redirectUri, UriKind.Absolute, out var requested))
                return false;

            foreach (var registeredUri in client.RedirectUris)
            {
                if (!Uri.TryCreate(registeredUri, UriKind.Absolute, out var registered))
                    continue;

                if (!string.Equals(requested.Scheme, registered.Scheme, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(requested.Host, registered.Host, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(requested.AbsolutePath, registered.AbsolutePath, StringComparison.Ordinal))
                    continue;

                var anyPort = client.IsPublicClient && requested.Scheme == Uri.UriSchemeHttp && IsLoopback(requested.Host);
                if (anyPort || requested.Port == registered.Port)
                    return true;
            }

            return false;
        }

        public static string BuildAuthorizationResponse(string redirectUri, string code, string state)
        {
            var separator = redirectUri.Contains('?') ? "&" : "?";
            var url = $"{redirectUri}{separator}code={Uri.EscapeDataString(code)}";
            return string.IsNullOrEmpty(state) ? url : $"{url}&state={Uri.EscapeDataString(state)}";
        }

        private static bool IsLoopback(string host) =>
            string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1" || host == "[::1]";
    }
}
