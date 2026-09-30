using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GoogleTracer;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApp.News;

namespace WebApp.Controllers
{
    /// <summary>Public news feed. Anyone can read published posts and react anonymously (one reaction of each kind per browser).</summary>
    [ApiController]
    [Route("api/news")]
    [Profile]
    public class NewsController : ControllerBase
    {
        public const string VisitorCookieName = "passi_news_visitor";
        private static readonly Regex VisitorIdPattern = new("^[0-9a-f]{32}$", RegexOptions.Compiled);

        private readonly NewsService _news;
        private readonly NewsAdmins _admins;
        private readonly IAntiforgery _antiforgery;
        private readonly NewsReactionRateLimiter _rateLimiter;

        public NewsController(NewsService news, NewsAdmins admins, IAntiforgery antiforgery, NewsReactionRateLimiter rateLimiter)
        {
            _news = news;
            _admins = admins;
            _antiforgery = antiforgery;
            _rateLimiter = rateLimiter;
        }

        /// <summary>Antiforgery token for reaction/admin writes, and whether to show the admin UI.</summary>
        [HttpGet("context")]
        public IActionResult Context()
        {
            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(new
            {
                csrfToken = tokens.RequestToken,
                csrfHeaderName = tokens.HeaderName,
                isAdmin = _admins.IsAdmin(User),
            });
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int skip = 0, [FromQuery] int take = 10) =>
            Ok(await _news.ListPublishedAsync(skip, take, ExistingVisitorId()));

        [HttpGet("{slug}")]
        public async Task<IActionResult> Get(string slug)
        {
            var post = await _news.GetPublishedAsync(slug, ExistingVisitorId());
            return post == null ? NotFound() : Ok(post);
        }

        [HttpPut("{slug}/reactions/{reaction}")]
        public Task<IActionResult> React(string slug, string reaction) => SetReaction(slug, reaction, true);

        [HttpDelete("{slug}/reactions/{reaction}")]
        public Task<IActionResult> Unreact(string slug, string reaction) => SetReaction(slug, reaction, false);

        private async Task<IActionResult> SetReaction(string slug, string reaction, bool on)
        {
            if (!_rateLimiter.TryAcquire(HttpContext))
                return StatusCode(StatusCodes.Status429TooManyRequests, new { errors = "Too many reactions, try again in a minute" });

            try
            {
                return Ok(await _news.SetReactionAsync(slug, reaction, EnsureVisitorId(), on));
            }
            catch (NewsValidationException e)
            {
                return BadRequest(new { errors = e.Message });
            }
            catch (NewsNotFoundException)
            {
                return NotFound();
            }
        }

        private string ExistingVisitorId()
        {
            var id = Request.Cookies[VisitorCookieName];
            return id != null && VisitorIdPattern.IsMatch(id) ? id : null;
        }

        private string EnsureVisitorId()
        {
            var id = ExistingVisitorId();
            if (id != null)
                return id;

            id = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            Response.Cookies.Append(VisitorCookieName, id, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromDays(365),
            });
            return id;
        }
    }
}
