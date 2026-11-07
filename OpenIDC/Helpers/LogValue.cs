namespace OpenIDC.Helpers
{
    internal static class LogValue
    {
        // Keep untrusted values on one line in text logs while preserving their content for diagnostics.
        internal static string Safe(string value) => value?.Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}
