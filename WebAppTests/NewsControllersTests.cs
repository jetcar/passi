using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
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
    public class NewsControllersTests
    {
        private const string Admin = "admin@passi.cloud";

        private SqliteConnection _connection;
        private WebAppDbContext _db;
        private NewsService _service;
        private NewsAdmins _admins;
        private NewsReactionRateLimiter _limiter;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _db = new WebAppDbContext(new DbContextOptionsBuilder<WebAppDbContext>().UseSqlite(_connection).Options);
            _db.Database.EnsureCreated();
            _service = new NewsService(_db);
            _admins = new NewsAdmins(new AppSetting(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> { ["AppSetting:NewsAdminEmails"] = "Admin@Passi.cloud; other@passi.cloud" })
                .Build()) { PrefferAppsettingFile = true });
            _limiter = new NewsReactionRateLimiter(maxPerWindow: 3, window: TimeSpan.FromMinutes(1));
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private static DefaultHttpContext Http(string email = null, string cookie = null, string forwardedFor = null)
        {
            var identity = email == null
                ? new ClaimsIdentity()
                : new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, email), new Claim(ClaimTypes.NameIdentifier, email) }, "Cookies");
            var http = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
            if (cookie != null)
                http.Request.Headers["Cookie"] = $"{NewsController.VisitorCookieName}={cookie}";
            if (forwardedFor != null)
                http.Request.Headers["X-Forwarded-For"] = forwardedFor;
            return http;
        }

        private NewsAdminController AdminController(string email) =>
            new(_service, _admins) { ControllerContext = new ControllerContext { HttpContext = Http(email) } };

        private NewsController PublicController(DefaultHttpContext http) =>
            new(_service, _admins, new FakeAntiforgery(), _limiter) { ControllerContext = new ControllerContext { HttpContext = http } };

        private async Task<string> PublishedSlug()
        {
            var post = await _service.CreateAsync(new NewsPostInput { Title = "Launch", Summary = "s", BodyMarkdown = "b" }, Admin);
            await _service.SetPublishedAsync(post.Slug, true);
            return post.Slug;
        }

        [Test]
        public void AdminsAreMatchedCaseInsensitivelyByEmailClaim()
        {
            Assert.That(_admins.IsAdmin(Http(Admin).User), Is.True);
            Assert.That(_admins.IsAdmin(Http("OTHER@passi.cloud").User), Is.True);
            Assert.That(_admins.IsAdmin(Http("mallory@passi.cloud").User), Is.False);
            Assert.That(_admins.IsAdmin(Http().User), Is.False);
        }

        [Test]
        public void JwtStyleEmailClaimIsAlsoAccepted()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("email", Admin) }, "Bearer"));
            Assert.That(_admins.IsAdmin(user), Is.True);
        }

        [Test]
        public async Task AdminEndpointsRejectAnonymousAndNonAdmins()
        {
            Assert.That(await AdminController(null).List(), Is.InstanceOf<UnauthorizedResult>());
            Assert.That(await AdminController("mallory@passi.cloud").List(), Is.InstanceOf<ForbidResult>());
            Assert.That(await AdminController("mallory@passi.cloud").Create(new NewsPostInput { Title = "x" }), Is.InstanceOf<ForbidResult>());
            Assert.That(await _db.NewsPosts.CountAsync(), Is.Zero);
        }

        [Test]
        public async Task AdminCanCreatePublishAndListDrafts()
        {
            var created = (OkObjectResult)await AdminController(Admin).Create(new NewsPostInput { Title = "Hello", Summary = "s", BodyMarkdown = "b" });
            var post = (NewsPost)created.Value;
            Assert.That(post.AuthorEmail, Is.EqualTo(Admin));

            var list = (OkObjectResult)await AdminController(Admin).List();
            Assert.That(((List<NewsPost>)list.Value).Single().PublishedAt, Is.Null);

            var published = (OkObjectResult)await AdminController(Admin).Publish(post.Slug);
            Assert.That(((NewsPost)published.Value).PublishedAt, Is.Not.Null);
        }

        [Test]
        public async Task AdminValidationErrorsAreBadRequestAndMissingPostsAreNotFound()
        {
            Assert.That(await AdminController(Admin).Create(new NewsPostInput { Title = "" }), Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(await AdminController(Admin).Publish("missing"), Is.InstanceOf<NotFoundObjectResult>());
            Assert.That(await AdminController(Admin).Delete("missing"), Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task PublicDetailOfDraftIsNotFound()
        {
            var draft = await _service.CreateAsync(new NewsPostInput { Title = "Secret" }, Admin);

            Assert.That(await PublicController(Http()).Get(draft.Slug), Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task ReactingIssuesVisitorCookieOnce()
        {
            var slug = await PublishedSlug();
            var http = Http();

            var result = (OkObjectResult)await PublicController(http).React(slug, "like");

            Assert.That(((ReactionState)result.Value).Counts["like"], Is.EqualTo(1));
            var setCookie = http.Response.Headers["Set-Cookie"].ToString();
            Assert.That(setCookie, Does.Contain(NewsController.VisitorCookieName + "="));
            Assert.That(setCookie.ToLowerInvariant(), Does.Contain("httponly"));
        }

        [Test]
        public async Task ExistingVisitorCookieIsReusedSoReactionsDeduplicate()
        {
            var slug = await PublishedSlug();
            var visitor = new string('a', 32);

            await PublicController(Http(cookie: visitor)).React(slug, "like");
            var result = (OkObjectResult)await PublicController(Http(cookie: visitor)).React(slug, "like");

            Assert.That(((ReactionState)result.Value).Counts["like"], Is.EqualTo(1));

            var removed = (OkObjectResult)await PublicController(Http(cookie: visitor)).Unreact(slug, "like");
            Assert.That(((ReactionState)removed.Value).Counts["like"], Is.Zero);
        }

        [Test]
        public async Task MalformedVisitorCookieIsReplaced()
        {
            var slug = await PublishedSlug();
            var http = Http(cookie: "not-a-valid-id'; drop table");

            await PublicController(http).React(slug, "like");

            Assert.That(await _db.NewsReactions.Select(r => r.VisitorId).SingleAsync(), Does.Match("^[0-9a-f]{32}$"));
        }

        [Test]
        public async Task ReactionsAreRateLimitedPerClientIp()
        {
            var slug = await PublishedSlug();

            for (var i = 0; i < 3; i++)
                Assert.That(await PublicController(Http(forwardedFor: "1.1.1.1, 9.9.9.9")).React(slug, "like"), Is.InstanceOf<OkObjectResult>());

            var limited = await PublicController(Http(forwardedFor: "1.1.1.1, 9.9.9.9")).React(slug, "like");
            Assert.That((limited as ObjectResult)?.StatusCode ?? (limited as StatusCodeResult)?.StatusCode, Is.EqualTo(429));

            Assert.That(await PublicController(Http(forwardedFor: "1.1.1.1, 8.8.8.8")).React(slug, "like"), Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task UnknownReactionIsBadRequestAndUnknownPostIsNotFound()
        {
            var slug = await PublishedSlug();

            Assert.That(await PublicController(Http()).React(slug, "poop"), Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(await PublicController(Http()).React("missing", "like"), Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void ContextReportsAdminFlag()
        {
            dynamic admin = ((OkObjectResult)PublicController(Http(Admin)).Context()).Value;
            dynamic anon = ((OkObjectResult)PublicController(Http()).Context()).Value;

            Assert.That((bool)admin.GetType().GetProperty("isAdmin").GetValue(admin), Is.True);
            Assert.That((bool)anon.GetType().GetProperty("isAdmin").GetValue(anon), Is.False);
        }
    }
}
