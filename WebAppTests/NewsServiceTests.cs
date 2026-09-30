using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using WebApp;
using WebApp.News;

namespace WebAppTests
{
    public class NewsServiceTests
    {
        private SqliteConnection _connection;
        private WebAppDbContext _db;
        private NewsService _service;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _db = new WebAppDbContext(new DbContextOptionsBuilder<WebAppDbContext>().UseSqlite(_connection).Options);
            _db.Database.EnsureCreated();
            _now = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
            _service = new NewsService(_db, () => _now);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private static NewsPostInput Input(string title = "Hello world", string slug = null, string summary = "Short", string body = "Body **bold**") =>
            new NewsPostInput { Title = title, Slug = slug, Summary = summary, BodyMarkdown = body };

        [Test]
        public async Task CreateGeneratesSlugFromTitleAndStartsAsDraft()
        {
            var post = await _service.CreateAsync(Input("Passi 2.0: Faster Logins!"), "admin@passi.cloud");

            Assert.That(post.Slug, Is.EqualTo("passi-2-0-faster-logins"));
            Assert.That(post.PublishedAt, Is.Null);
            Assert.That(post.AuthorEmail, Is.EqualTo("admin@passi.cloud"));
        }

        [Test]
        public async Task CreateMakesDuplicateSlugsUnique()
        {
            await _service.CreateAsync(Input("Release"), "a@b.c");
            var second = await _service.CreateAsync(Input("Release"), "a@b.c");

            Assert.That(second.Slug, Is.EqualTo("release-2"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void CreateRejectsEmptyTitle(string title)
        {
            Assert.ThrowsAsync<NewsValidationException>(() => _service.CreateAsync(Input(title), "a@b.c"));
        }

        [TestCase("context")]
        [TestCase("admin")]
        [TestCase("rss.xml")]
        [TestCase("Bad Slug")]
        public void CreateRejectsReservedOrInvalidExplicitSlug(string slug)
        {
            Assert.ThrowsAsync<NewsValidationException>(() => _service.CreateAsync(Input(slug: slug), "a@b.c"));
        }

        [Test]
        public async Task DraftsAreHiddenFromPublicUntilPublished()
        {
            var post = await _service.CreateAsync(Input(), "a@b.c");

            Assert.That(await _service.ListPublishedAsync(0, 10, "v1"), Is.Empty);
            Assert.That(await _service.GetPublishedAsync(post.Slug, "v1"), Is.Null);

            await _service.SetPublishedAsync(post.Slug, true);

            var list = await _service.ListPublishedAsync(0, 10, "v1");
            Assert.That(list.Select(p => p.Slug), Is.EqualTo(new[] { post.Slug }));
            Assert.That(list[0].PublishedAt, Is.EqualTo(_now));
        }

        [Test]
        public async Task PublishedListIsNewestFirstAndPaged()
        {
            foreach (var title in new[] { "First", "Second", "Third" })
            {
                var p = await _service.CreateAsync(Input(title), "a@b.c");
                await _service.SetPublishedAsync(p.Slug, true);
                _now = _now.AddDays(1);
            }

            var page = await _service.ListPublishedAsync(0, 2, "v1");
            Assert.That(page.Select(p => p.Title), Is.EqualTo(new[] { "Third", "Second" }));
            Assert.That((await _service.ListPublishedAsync(2, 2, "v1")).Select(p => p.Title), Is.EqualTo(new[] { "First" }));
        }

        [Test]
        public async Task UnpublishHidesPostAgain()
        {
            var post = await _service.CreateAsync(Input(), "a@b.c");
            await _service.SetPublishedAsync(post.Slug, true);
            await _service.SetPublishedAsync(post.Slug, false);

            Assert.That(await _service.GetPublishedAsync(post.Slug, "v1"), Is.Null);
        }

        [Test]
        public async Task DetailRendersMarkdownToHtml()
        {
            var post = await _service.CreateAsync(Input(body: "Hello **world**"), "a@b.c");
            await _service.SetPublishedAsync(post.Slug, true);

            var detail = await _service.GetPublishedAsync(post.Slug, "v1");
            Assert.That(detail.Html, Does.Contain("<strong>world</strong>"));
        }

        [Test]
        public async Task UpdateChangesFieldsAndKeepsSlugUnlessGiven()
        {
            var post = await _service.CreateAsync(Input("Old title"), "a@b.c");

            var updated = await _service.UpdateAsync(post.Slug, Input("New title", summary: "New summary", body: "New body"));

            Assert.That(updated.Slug, Is.EqualTo("old-title"));
            Assert.That(updated.Title, Is.EqualTo("New title"));
            Assert.That(updated.Summary, Is.EqualTo("New summary"));
            Assert.That(updated.BodyMarkdown, Is.EqualTo("New body"));
        }

        [Test]
        public async Task UpdateOfMissingPostThrowsNotFound()
        {
            Assert.ThrowsAsync<NewsNotFoundException>(() => _service.UpdateAsync("nope", Input()));
            await Task.CompletedTask;
        }

        [Test]
        public async Task DeleteRemovesPostAndItsReactions()
        {
            var post = await _service.CreateAsync(Input(), "a@b.c");
            await _service.SetPublishedAsync(post.Slug, true);
            await _service.SetReactionAsync(post.Slug, "like", "v1", true);

            await _service.DeleteAsync(post.Slug);

            Assert.That(await _service.ListAllAsync(), Is.Empty);
            Assert.That(await _db.NewsReactions.CountAsync(), Is.Zero);
        }

        [Test]
        public async Task ReactionsAreCountedOncePerVisitorAndCanBeRemoved()
        {
            var post = await _service.CreateAsync(Input(), "a@b.c");
            await _service.SetPublishedAsync(post.Slug, true);

            await _service.SetReactionAsync(post.Slug, "like", "v1", true);
            await _service.SetReactionAsync(post.Slug, "like", "v1", true);
            await _service.SetReactionAsync(post.Slug, "like", "v2", true);
            var state = await _service.SetReactionAsync(post.Slug, "rocket", "v1", true);

            Assert.That(state.Counts["like"], Is.EqualTo(2));
            Assert.That(state.Counts["rocket"], Is.EqualTo(1));
            Assert.That(state.Mine, Is.EquivalentTo(new[] { "like", "rocket" }));

            state = await _service.SetReactionAsync(post.Slug, "like", "v1", false);
            Assert.That(state.Counts["like"], Is.EqualTo(1));
            Assert.That(state.Mine, Is.EquivalentTo(new[] { "rocket" }));

            var detail = await _service.GetPublishedAsync(post.Slug, "v2");
            Assert.That(detail.Reactions.Counts["like"], Is.EqualTo(1));
            Assert.That(detail.Reactions.Mine, Is.EquivalentTo(new[] { "like" }));
        }

        [Test]
        public async Task CountsIncludeEveryAllowedReactionEvenWhenZero()
        {
            var post = await _service.CreateAsync(Input(), "a@b.c");
            await _service.SetPublishedAsync(post.Slug, true);

            var summary = (await _service.ListPublishedAsync(0, 10, "v1")).Single();
            Assert.That(summary.Reactions.Counts.Keys, Is.EquivalentTo(NewsReactions.Allowed));
            Assert.That(summary.Reactions.Counts.Values, Is.All.Zero);
        }

        [Test]
        public async Task UnknownReactionIsRejected()
        {
            var post = await _service.CreateAsync(Input(), "a@b.c");
            await _service.SetPublishedAsync(post.Slug, true);

            Assert.ThrowsAsync<NewsValidationException>(() => _service.SetReactionAsync(post.Slug, "poop", "v1", true));
        }

        [Test]
        public async Task CannotReactToDraft()
        {
            var post = await _service.CreateAsync(Input(), "a@b.c");

            Assert.ThrowsAsync<NewsNotFoundException>(() => _service.SetReactionAsync(post.Slug, "like", "v1", true));
        }
    }
}
