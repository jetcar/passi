using System;
using System.Linq;
using OpenIDC.Models;

namespace OpenIDC.Helpers
{
    public static class RedirectUriHelper
    {
        /// <summary>
        /// Redirect URIs must match a registered URI exactly. The one
        /// exception is RFC 8252 section 7.3: for public clients, an http loopback URI may use any port, because native
        /// apps and CLI agents listen on an ephemeral port.
        /// </summary>
        public static bool IsAllowed(string redirectUri, OidcClient client)
        {
            if (string.IsNullOrEmpty(redirectUri) || client?.RedirectUris == null ||
                !Uri.TryCreate(redirectUri, UriKind.Absolute, out var requested) ||
                !string.IsNullOrEmpty(requested.UserInfo) || !string.IsNullOrEmpty(requested.Fragment))
                return false;

            if (client?.IsDynamic == true)
                return client.RedirectUris.Contains(redirectUri, StringComparer.Ordinal);

            foreach (var registeredUri in client.RedirectUris)
            {
                if (!Uri.TryCreate(registeredUri, UriKind.Absolute, out var registered) ||
                    !string.IsNullOrEmpty(registered.UserInfo) || !string.IsNullOrEmpty(registered.Fragment))
                    continue;

                if (string.Equals(redirectUri, registeredUri, StringComparison.Ordinal))
                    return true;

                var anyPort = client.IsPublicClient && requested.Scheme == Uri.UriSchemeHttp &&
                    registered.Scheme == Uri.UriSchemeHttp && IsLoopback(requested.Host) &&
                    string.Equals(requested.Host, registered.Host, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(requested.PathAndQuery, registered.PathAndQuery, StringComparison.Ordinal);
                if (anyPort)
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

        public static string BuildAuthorizationErrorResponse(string redirectUri, string error, string description, string state)
        {
            var separator = redirectUri.Contains('?') ? "&" : "?";
            var url = $"{redirectUri}{separator}error={Uri.EscapeDataString(error)}";
            if (!string.IsNullOrEmpty(description))
                url += $"&error_description={Uri.EscapeDataString(description)}";
            return string.IsNullOrEmpty(state) ? url : $"{url}&state={Uri.EscapeDataString(state)}";
        }

        private static bool IsLoopback(string host) =>
            string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1" || host == "[::1]";
    }
}
