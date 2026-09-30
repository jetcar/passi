using NUnit.Framework;
using WebApp.News;

namespace WebAppTests
{
    public class NewsMarkdownTests
    {
        [Test]
        public void RendersBasicMarkdown()
        {
            var html = NewsMarkdown.ToHtml("# Title\n\n- item\n\n[link](https://passi.cloud)");

            Assert.That(html, Does.Contain("<h1"));
            Assert.That(html, Does.Contain("<li>item</li>"));
            Assert.That(html, Does.Contain("href=\"https://passi.cloud\""));
        }

        [Test]
        public void RawHtmlIsEscapedNotRendered()
        {
            var html = NewsMarkdown.ToHtml("<script>alert(1)</script> <img src=x onerror=alert(1)>");

            Assert.That(html, Does.Not.Contain("<script"));
            Assert.That(html, Does.Not.Contain("<img"));
        }

        [TestCase("[x](javascript:alert(1))")]
        [TestCase("[x](JavaScript:alert(1))")]
        [TestCase("[x](data:text/html;base64,PHNjcmlwdD4=)")]
        [TestCase("![x](javascript:alert(1))")]
        [TestCase("<javascript:alert(1)>")]
        public void UnsafeLinkSchemesAreDropped(string markdown)
        {
            var html = NewsMarkdown.ToHtml(markdown);

            Assert.That(html, Does.Not.Match("(?i)(href|src)=\"\\s*(javascript|data):"));
        }

        [Test]
        public void ExternalLinksOpenSafely()
        {
            var html = NewsMarkdown.ToHtml("[gh](https://github.com/jetcar/passi)");

            Assert.That(html, Does.Contain("rel=\"noopener noreferrer nofollow\""));
        }
    }
}
