using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace WebApp.News
{
    /// <summary>News/blog posts and their anonymous reactions. Shared by the web API and the MCP tools.</summary>
    public class NewsService
    {
        public const int MaxTitleLength = 200;
        public const int MaxSlugLength = 80;
        public const int MaxSummaryLength = 500;
        public const int MaxBodyLength = 100_000;

        private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

        /// <summary>Slugs that collide with literal routes under /api/news and /news.</summary>
        private static readonly HashSet<string> ReservedSlugs = new() { "context", "admin", "rss" };

        private readonly WebAppDbContext _db;
        private readonly Func<DateTime> _utcNow;

        public NewsService(WebAppDbContext db) : this(db, () => DateTime.UtcNow) { }

        public NewsService(WebAppDbContext db, Func<DateTime> utcNow)
        {
            _db = db;
            _utcNow = utcNow;
        }

        public async Task<List<NewsPostSummary>> ListPublishedAsync(int skip, int take, string visitorId)
        {
            var posts = await _db.NewsPosts.AsNoTracking()
                .Where(p => p.PublishedAt != null)
                .OrderByDescending(p => p.PublishedAt)
                .Skip(Math.Max(0, skip))
                .Take(Math.Clamp(take, 1, 50))
                .ToListAsync();

            var reactions = await ReactionStatesAsync(posts.Select(p => p.Id).ToList(), visitorId);
            return posts.Select(p => ToSummary(new NewsPostSummary(), p, reactions[p.Id])).ToList();
        }

        public async Task<NewsPostDetail> GetPublishedAsync(string slug, string visitorId)
        {
            var post = await _db.NewsPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug && p.PublishedAt != null);
            if (post == null)
                return null;

            var reactions = await ReactionStatesAsync(new List<Guid> { post.Id }, visitorId);
            var detail = ToSummary(new NewsPostDetail(), post, reactions[post.Id]);
            detail.Html = NewsMarkdown.ToHtml(post.BodyMarkdown);
            return detail;
        }

        public async Task<ReactionState> SetReactionAsync(string slug, string reaction, string visitorId, bool on)
        {
            if (!NewsReactions.Allowed.Contains(reaction))
                throw new NewsValidationException($"Unknown reaction '{reaction}'");
            if (string.IsNullOrEmpty(visitorId))
                throw new NewsValidationException("Visitor id is required");

            var post = await _db.NewsPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug && p.PublishedAt != null)
                       ?? throw new NewsNotFoundException(slug);

            var existing = await _db.NewsReactions.FindAsync(post.Id, visitorId, reaction);
            if (on && existing == null)
            {
                _db.NewsReactions.Add(new NewsReaction { PostId = post.Id, VisitorId = visitorId, Reaction = reaction, CreatedAt = _utcNow() });
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // A concurrent request from the same visitor inserted it first; the end state is the same.
                    _db.ChangeTracker.Clear();
                }
            }
            else if (!on && existing != null)
            {
                _db.NewsReactions.Remove(existing);
                await _db.SaveChangesAsync();
            }

            return (await ReactionStatesAsync(new List<Guid> { post.Id }, visitorId))[post.Id];
        }

        public Task<List<NewsPost>> ListAllAsync() =>
            _db.NewsPosts.AsNoTracking()
                .OrderByDescending(p => p.PublishedAt == null)
                .ThenByDescending(p => p.PublishedAt)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();

        public async Task<NewsPost> GetAsync(string slug) =>
            await _db.NewsPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Slug == slug) ?? throw new NewsNotFoundException(slug);

        public async Task<NewsPost> CreateAsync(NewsPostInput input, string authorEmail)
        {
            Validate(input);

            string slug;
            if (string.IsNullOrWhiteSpace(input.Slug))
            {
                slug = await UniqueSlugAsync(Slugify(input.Title));
            }
            else
            {
                slug = ValidateSlug(input.Slug);
                if (await _db.NewsPosts.AnyAsync(p => p.Slug == slug))
                    throw new NewsValidationException($"Slug '{slug}' is already used");
            }

            var now = _utcNow();
            var post = new NewsPost
            {
                Id = Guid.NewGuid(),
                Slug = slug,
                Title = input.Title.Trim(),
                Summary = input.Summary?.Trim() ?? "",
                BodyMarkdown = input.BodyMarkdown ?? "",
                CreatedAt = now,
                UpdatedAt = now,
                AuthorEmail = authorEmail,
            };
            _db.NewsPosts.Add(post);
            await _db.SaveChangesAsync();
            return post;
        }

        public async Task<NewsPost> UpdateAsync(string slug, NewsPostInput input)
        {
            Validate(input);

            var post = await _db.NewsPosts.FirstOrDefaultAsync(p => p.Slug == slug) ?? throw new NewsNotFoundException(slug);

            if (!string.IsNullOrWhiteSpace(input.Slug) && input.Slug.Trim() != post.Slug)
            {
                var newSlug = ValidateSlug(input.Slug);
                if (await _db.NewsPosts.AnyAsync(p => p.Slug == newSlug))
                    throw new NewsValidationException($"Slug '{newSlug}' is already used");
                post.Slug = newSlug;
            }

            post.Title = input.Title.Trim();
            post.Summary = input.Summary?.Trim() ?? "";
            post.BodyMarkdown = input.BodyMarkdown ?? "";
            post.UpdatedAt = _utcNow();
            await _db.SaveChangesAsync();
            return post;
        }

        public async Task<NewsPost> SetPublishedAsync(string slug, bool published)
        {
            var post = await _db.NewsPosts.FirstOrDefaultAsync(p => p.Slug == slug) ?? throw new NewsNotFoundException(slug);

            if (published && post.PublishedAt == null)
                post.PublishedAt = _utcNow();
            else if (!published)
                post.PublishedAt = null;

            post.UpdatedAt = _utcNow();
            await _db.SaveChangesAsync();
            return post;
        }

        public async Task DeleteAsync(string slug)
        {
            var post = await _db.NewsPosts.FirstOrDefaultAsync(p => p.Slug == slug) ?? throw new NewsNotFoundException(slug);

            _db.NewsReactions.RemoveRange(_db.NewsReactions.Where(r => r.PostId == post.Id));
            _db.NewsPosts.Remove(post);
            await _db.SaveChangesAsync();
        }

        private async Task<Dictionary<Guid, ReactionState>> ReactionStatesAsync(List<Guid> postIds, string visitorId)
        {
            var counts = await _db.NewsReactions.AsNoTracking()
                .Where(r => postIds.Contains(r.PostId))
                .GroupBy(r => new { r.PostId, r.Reaction })
                .Select(g => new { g.Key.PostId, g.Key.Reaction, Count = g.Count() })
                .ToListAsync();

            var mine = string.IsNullOrEmpty(visitorId)
                ? new List<NewsReaction>()
                : await _db.NewsReactions.AsNoTracking()
                    .Where(r => postIds.Contains(r.PostId) && r.VisitorId == visitorId)
                    .ToListAsync();

            return postIds.ToDictionary(id => id, id => new ReactionState
            {
                Counts = NewsReactions.Allowed.ToDictionary(
                    reaction => reaction,
                    reaction => counts.FirstOrDefault(c => c.PostId == id && c.Reaction == reaction)?.Count ?? 0),
                Mine = mine.Where(r => r.PostId == id).Select(r => r.Reaction).ToList(),
            });
        }

        private static T ToSummary<T>(T target, NewsPost post, ReactionState reactions) where T : NewsPostSummary
        {
            target.Slug = post.Slug;
            target.Title = post.Title;
            target.Summary = post.Summary;
            target.PublishedAt = post.PublishedAt;
            target.Reactions = reactions;
            return target;
        }

        private static void Validate(NewsPostInput input)
        {
            if (input == null || string.IsNullOrWhiteSpace(input.Title))
                throw new NewsValidationException("Title is required");
            if (input.Title.Trim().Length > MaxTitleLength)
                throw new NewsValidationException($"Title must be at most {MaxTitleLength} characters");
            if ((input.Summary?.Trim().Length ?? 0) > MaxSummaryLength)
                throw new NewsValidationException($"Summary must be at most {MaxSummaryLength} characters");
            if ((input.BodyMarkdown?.Length ?? 0) > MaxBodyLength)
                throw new NewsValidationException($"Body must be at most {MaxBodyLength} characters");
        }

        private static string ValidateSlug(string slug)
        {
            var trimmed = slug.Trim();
            if (trimmed.Length > MaxSlugLength || !SlugPattern.IsMatch(trimmed))
                throw new NewsValidationException("Slug must be lowercase letters, digits and single dashes (max 80 characters)");
            if (ReservedSlugs.Contains(trimmed))
                throw new NewsValidationException($"Slug '{trimmed}' is reserved");
            return trimmed;
        }

        private static string Slugify(string title)
        {
            var builder = new StringBuilder();
            foreach (var c in title.Trim().ToLowerInvariant())
            {
                if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
                    builder.Append(c);
                else if (builder.Length > 0 && builder[^1] != '-')
                    builder.Append('-');
            }

            var slug = builder.ToString().Trim('-');
            if (slug.Length > MaxSlugLength - 4)
                slug = slug.Substring(0, MaxSlugLength - 4).Trim('-');
            if (slug.Length == 0 || ReservedSlugs.Contains(slug))
                slug = "post" + (slug.Length == 0 ? "" : "-" + slug);
            return slug;
        }

        private async Task<string> UniqueSlugAsync(string baseSlug)
        {
            var slug = baseSlug;
            for (var i = 2; await _db.NewsPosts.AnyAsync(p => p.Slug == slug); i++)
                slug = $"{baseSlug}-{i}";
            return slug;
        }
    }
}
