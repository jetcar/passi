using System;
using System.Net;

namespace Services
{
    internal sealed record PassiEmailContent(string Subject, string PlainText, string Html);

    internal static class PassiEmailTemplate
    {
        internal static PassiEmailContent CreateConfirmationCode(string code)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);

            var subjectCode = code.Replace("\r", string.Empty).Replace("\n", string.Empty);
            var htmlCode = WebUtility.HtmlEncode(code);

            return new PassiEmailContent(
                $"Passi confirmation code: {subjectCode}",
                $"Your Passi confirmation code is: {code}\n\nEnter this code in Passi to continue.\n\nIf you did not request this code, you can ignore this email.",
                $$"""
                <!doctype html>
                <html lang="en">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width, initial-scale=1">
                  <title>Passi confirmation code</title>
                </head>
                <body style="margin:0;background:#f6f7f9;color:#172033;font-family:Arial,sans-serif;">
                  <main style="max-width:520px;margin:0 auto;padding:40px 20px;">
                    <section style="background:#ffffff;border-radius:16px;padding:32px;text-align:center;">
                      <h1 style="margin:0 0 16px;font-size:24px;">Confirm your request</h1>
                      <p style="margin:0 0 24px;">Enter this code in Passi to continue.</p>
                      <p style="margin:0 0 24px;font-size:32px;font-weight:700;letter-spacing:6px;">{{htmlCode}}</p>
                      <p style="margin:0;color:#5f6878;font-size:14px;">If you did not request this code, you can ignore this email.</p>
                    </section>
                  </main>
                </body>
                </html>
                """);
        }
    }
}
