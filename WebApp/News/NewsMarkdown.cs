using System;
using System.IO;
using System.Linq;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace WebApp.News
{
    /// <summary>
    /// Renders post Markdown to HTML that is safe to inject into the page: raw HTML is escaped and
    /// links/images may only use http(s), mailto or relative URLs.
    /// </summary>
    public static class NewsMarkdown
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UsePipeTables()
            .UseEmphasisExtras()
            .UseTaskLists()
            .UseAutoLinks()
            .Build();

        public static string ToHtml(string markdown)
        {
            var document = Markdown.Parse(markdown ?? "", Pipeline);

            foreach (var link in document.Descendants<LinkInline>().ToList())
            {
                if (!IsSafeUrl(link.Url))
                {
                    link.Url = "#";
                }
                else if (!link.IsImage && IsAbsoluteHttp(link.Url))
                {
                    link.GetAttributes().AddPropertyIfNotExist("rel", "noopener noreferrer nofollow");
                }
            }

            foreach (var autolink in document.Descendants<AutolinkInline>().ToList())
            {
                if (!autolink.IsEmail && !IsSafeUrl(autolink.Url))
                {
                    autolink.ReplaceBy(new LiteralInline(autolink.Url));
                }
            }

            using var writer = new StringWriter();
            var renderer = new HtmlRenderer(writer);
            Pipeline.Setup(renderer);
            renderer.Render(document);
            writer.Flush();
            return writer.ToString();
        }

        private static bool IsAbsoluteHttp(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        private static bool IsSafeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true;

            var trimmed = url.Trim();
            var colon = trimmed.IndexOf(':');
            var firstDelimiter = trimmed.IndexOfAny(new[] { '/', '?', '#' });
            var hasScheme = colon > 0 && (firstDelimiter < 0 || colon < firstDelimiter);
            if (!hasScheme)
                return true;

            var scheme = trimmed.Substring(0, colon).ToLowerInvariant();
            return scheme is "http" or "https" or "mailto";
        }
    }
}
