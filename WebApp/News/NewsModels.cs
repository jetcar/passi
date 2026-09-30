using System;
using System.Collections.Generic;

namespace WebApp.News
{
    public class NewsPost
    {
        public Guid Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string BodyMarkdown { get; set; }

        /// <summary>Null while the post is a draft.</summary>
        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string AuthorEmail { get; set; }
    }

    /// <summary>One anonymous visitor's reaction of one kind to one post.</summary>
    public class NewsReaction
    {
        public Guid PostId { get; set; }
        public string VisitorId { get; set; }
        public string Reaction { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class NewsReactions
    {
        /// <summary>The fixed reaction set; the front end maps each key to an emoji.</summary>
        public static readonly IReadOnlyList<string> Allowed = new[] { "like", "love", "celebrate", "rocket", "eyes" };
    }

    public class NewsPostInput
    {
        public string Title { get; set; }

        /// <summary>Optional; generated from the title on create when empty, kept unchanged on update when empty.</summary>
        public string Slug { get; set; }

        public string Summary { get; set; }
        public string BodyMarkdown { get; set; }
    }

    public class ReactionState
    {
        public Dictionary<string, int> Counts { get; set; } = new();

        /// <summary>Reactions the current visitor has given.</summary>
        public List<string> Mine { get; set; } = new();
    }

    public class NewsPostSummary
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public DateTime? PublishedAt { get; set; }
        public ReactionState Reactions { get; set; }
    }

    public class NewsPostDetail : NewsPostSummary
    {
        public string Html { get; set; }
    }

    public class NewsValidationException : Exception
    {
        public NewsValidationException(string message) : base(message) { }
    }

    public class NewsNotFoundException : Exception
    {
        public NewsNotFoundException(string slug) : base($"News post '{slug}' not found") { }
    }
}
