using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace WebApp.News
{
    /// <summary>MCP tools for news posts. The /mcp endpoint policy has already verified the caller is a news admin.</summary>
    [McpServerToolType]
    public class NewsMcpTools
    {
        private readonly NewsService _news;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public NewsMcpTools(NewsService news, IHttpContextAccessor httpContextAccessor)
        {
            _news = news;
            _httpContextAccessor = httpContextAccessor;
        }

        [McpServerTool(Name = "list_posts", ReadOnly = true, Title = "List news posts")]
        [Description("Lists all news posts, drafts first, then published newest first. Returns slug, title, summary and publish state.")]
        public Task<List<PostInfo>> ListPosts() =>
            Run(async () => (await _news.ListAllAsync()).Select(PostInfo.From).ToList());

        [McpServerTool(Name = "get_post", ReadOnly = true, Title = "Get news post")]
        [Description("Gets one news post by slug, including its Markdown body.")]
        public Task<PostInfo> GetPost([Description("The post slug")] string slug) =>
            Run(async () => PostInfo.From(await _news.GetAsync(slug), includeBody: true));

        [McpServerTool(Name = "create_post", Title = "Create news post")]
        [Description("Creates a news post. It stays a draft unless publish is true. The slug is generated from the title when omitted.")]
        public Task<PostInfo> CreatePost(
            [Description("Post title (max 200 characters)")] string title,
            [Description("Post body in Markdown. Raw HTML is not rendered.")] string body_markdown,
            [Description("One or two sentences shown on the home page and in the news list (max 500 characters)")] string summary = null,
            [Description("Optional URL slug: lowercase letters, digits and dashes")] string slug = null,
            [Description("Publish immediately instead of saving a draft")] bool publish = false) =>
            Run(async () =>
            {
                var post = await _news.CreateAsync(
                    new NewsPostInput { Title = title, Summary = summary, BodyMarkdown = body_markdown, Slug = slug },
                    NewsAdmins.EmailOf(_httpContextAccessor.HttpContext?.User));
                if (publish)
                    post = await _news.SetPublishedAsync(post.Slug, true);
                return PostInfo.From(post, includeBody: true);
            });

        [McpServerTool(Name = "update_post", Idempotent = true, Title = "Update news post")]
        [Description("Updates a news post. Only the fields you pass are changed. Does not change publish state.")]
        public Task<PostInfo> UpdatePost(
            [Description("Slug of the post to update")] string slug,
            [Description("New title")] string title = null,
            [Description("New summary")] string summary = null,
            [Description("New Markdown body")] string body_markdown = null,
            [Description("New slug; changes the post URL")] string new_slug = null) =>
            Run(async () =>
            {
                var current = await _news.GetAsync(slug);
                var post = await _news.UpdateAsync(slug, new NewsPostInput
                {
                    Title = title ?? current.Title,
                    Summary = summary ?? current.Summary,
                    BodyMarkdown = body_markdown ?? current.BodyMarkdown,
                    Slug = new_slug,
                });
                return PostInfo.From(post, includeBody: true);
            });

        [McpServerTool(Name = "publish_post", Idempotent = true, Title = "Publish news post")]
        [Description("Publishes a draft so it appears on the home page and /news. Keeps the original publish date if already published.")]
        public Task<PostInfo> PublishPost([Description("The post slug")] string slug) =>
            Run(async () => PostInfo.From(await _news.SetPublishedAsync(slug, true)));

        [McpServerTool(Name = "unpublish_post", Idempotent = true, Title = "Unpublish news post")]
        [Description("Turns a published post back into a draft, hiding it from the public.")]
        public Task<PostInfo> UnpublishPost([Description("The post slug")] string slug) =>
            Run(async () => PostInfo.From(await _news.SetPublishedAsync(slug, false)));

        [McpServerTool(Name = "delete_post", Destructive = true, Title = "Delete news post")]
        [Description("Permanently deletes a news post and its reactions.")]
        public Task<string> DeletePost([Description("The post slug")] string slug) =>
            Run(async () =>
            {
                await _news.DeleteAsync(slug);
                return $"Deleted '{slug}'";
            });

        /// <summary>Surfaces validation/not-found messages to the agent; the SDK hides other exception messages.</summary>
        private static async Task<T> Run<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (NewsValidationException e)
            {
                throw new McpException(e.Message);
            }
            catch (NewsNotFoundException e)
            {
                throw new McpException(e.Message);
            }
        }

        public class PostInfo
        {
            public string Slug { get; set; }
            public string Title { get; set; }
            public string Summary { get; set; }
            public string BodyMarkdown { get; set; }
            public bool Published { get; set; }
            public DateTime? PublishedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public string Url { get; set; }

            public static PostInfo From(NewsPost post) => From(post, includeBody: false);

            public static PostInfo From(NewsPost post, bool includeBody) => new()
            {
                Slug = post.Slug,
                Title = post.Title,
                Summary = post.Summary,
                BodyMarkdown = includeBody ? post.BodyMarkdown : null,
                Published = post.PublishedAt != null,
                PublishedAt = post.PublishedAt,
                UpdatedAt = post.UpdatedAt,
                Url = $"/news/{post.Slug}",
            };
        }
    }
}
