using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using ConfigurationManager;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using WebApp;
using WebApp.Controllers;
using WebApp.News;

namespace WebAppTests
{
    public class NewsFeedControllerTests
    {
        [Test]
        public async Task RssContainsOnlyPublishedPostsWithAbsoluteLinks()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            using var db = new WebAppDbContext(new DbContextOptionsBuilder<WebAppDbContext>().UseSqlite(connection).Options);
            db.Database.EnsureCreated();
            var service = new NewsService(db);

            var published = await service.CreateAsync(new NewsPostInput { Title = "Out now & <fast>", Summary = "Big release" }, "a@b.c");
            await service.SetPublishedAsync(published.Slug, true);
            await service.CreateAsync(new NewsPostInput { Title = "Secret draft" }, "a@b.c");

            var appSetting = new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["AppSetting:IdentityUrlBase"] = "https://passi.cloud/" })
                .Build()) { PrefferAppsettingFile = true };
            var controller = new NewsFeedController(service, appSetting) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

            var result = (ContentResult)await controller.Rss();

            Assert.That(result.ContentType, Does.StartWith("application/rss+xml"));
            var items = XDocument.Parse(result.Content).Descendants("item").ToList();
            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].Element("title")?.Value, Is.EqualTo("Out now & <fast>"));
            Assert.That(items[0].Element("link")?.Value, Is.EqualTo("https://passi.cloud/news/" + published.Slug));
            Assert.That(items[0].Element("description")?.Value, Is.EqualTo("Big release"));
        }
    }
}
