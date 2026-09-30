using System;
using System.Threading.Tasks;
using GoogleTracer;
using Microsoft.AspNetCore.Mvc;
using WebApp.News;

namespace WebApp.Controllers
{
    /// <summary>News authoring for signed-in admins (NewsAdminEmails). Drafts are only visible here.</summary>
    [ApiController]
    [Route("api/news/admin")]
    [Profile]
    public class NewsAdminController : ControllerBase
    {
        private readonly NewsService _news;
        private readonly NewsAdmins _admins;

        public NewsAdminController(NewsService news, NewsAdmins admins)
        {
            _news = news;
            _admins = admins;
        }

        [HttpGet("posts")]
        public Task<IActionResult> List() => AsAdmin(async () => Ok(await _news.ListAllAsync()));

        [HttpGet("posts/{slug}")]
        public Task<IActionResult> Get(string slug) => AsAdmin(async () => Ok(await _news.GetAsync(slug)));

        [HttpPost("posts")]
        public Task<IActionResult> Create([FromBody] NewsPostInput input) =>
            AsAdmin(async () => Ok(await _news.CreateAsync(input, NewsAdmins.EmailOf(User))));

        [HttpPut("posts/{slug}")]
        public Task<IActionResult> Update(string slug, [FromBody] NewsPostInput input) =>
            AsAdmin(async () => Ok(await _news.UpdateAsync(slug, input)));

        [HttpPost("posts/{slug}/publish")]
        public Task<IActionResult> Publish(string slug) => AsAdmin(async () => Ok(await _news.SetPublishedAsync(slug, true)));

        [HttpPost("posts/{slug}/unpublish")]
        public Task<IActionResult> Unpublish(string slug) => AsAdmin(async () => Ok(await _news.SetPublishedAsync(slug, false)));

        [HttpDelete("posts/{slug}")]
        public Task<IActionResult> Delete(string slug) => AsAdmin(async () =>
        {
            await _news.DeleteAsync(slug);
            return NoContent();
        });

        [HttpPost("preview")]
        public Task<IActionResult> Preview([FromBody] PreviewRequest request) =>
            AsAdmin(() => Task.FromResult<IActionResult>(Ok(new { html = NewsMarkdown.ToHtml(request?.BodyMarkdown) })));

        private async Task<IActionResult> AsAdmin(Func<Task<IActionResult>> action)
        {
            if (User.Identity?.IsAuthenticated != true)
                return Unauthorized();
            if (!_admins.IsAdmin(User))
                return Forbid();

            try
            {
                return await action();
            }
            catch (NewsValidationException e)
            {
                return BadRequest(new { errors = e.Message });
            }
            catch (NewsNotFoundException e)
            {
                return NotFound(new { errors = e.Message });
            }
        }

        public class PreviewRequest
        {
            public string BodyMarkdown { get; set; }
        }
    }
}
