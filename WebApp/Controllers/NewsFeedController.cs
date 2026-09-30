using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using ConfigurationManager;
using GoogleTracer;
using Microsoft.AspNetCore.Mvc;
using WebApp.News;

namespace WebApp.Controllers
{
    /// <summary>RSS 2.0 feed of published news posts at /news/rss.xml.</summary>
    [Profile]
    public class NewsFeedController : Controller
    {
        private readonly NewsService _news;
        private readonly string _publicBase;

        public NewsFeedController(NewsService news, AppSetting appSetting)
        {
            _news = news;
            _publicBase = (appSetting["PublicUrlBase"] ?? appSetting["IdentityUrlBase"])?.TrimEnd('/');
        }

        [HttpGet("news/rss.xml")]
        public async Task<IActionResult> Rss()
        {
            var posts = await _news.ListPublishedAsync(0, 20, null);

            var channel = new XElement("channel",
                new XElement("title", "Passi News"),
                new XElement("link", $"{_publicBase}/news"),
                new XElement("description", "Updates and releases from Passi, passwordless login approved on your phone."),
                posts.Select(post => new XElement("item",
                    new XElement("title", post.Title),
                    new XElement("link", $"{_publicBase}/news/{post.Slug}"),
                    new XElement("guid", new XAttribute("isPermaLink", "true"), $"{_publicBase}/news/{post.Slug}"),
                    new XElement("pubDate", post.PublishedAt?.ToString("R", CultureInfo.InvariantCulture)),
                    new XElement("description", post.Summary))));

            var document = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("rss", new XAttribute("version", "2.0"), channel));
            return Content(document.Declaration + "\n" + document.Root, "application/rss+xml; charset=utf-8");
        }
    }
}
