namespace SkythornLauncher.Models;

public sealed class BlogPostItem
{
    public string Title { get; init; } = string.Empty;
    public string Excerpt { get; init; } = string.Empty;
    public DateTime? PublishedDate { get; init; }
    public string Url { get; init; } = string.Empty;
}
