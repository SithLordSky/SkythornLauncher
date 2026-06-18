namespace SkythornLauncher.Models;

public sealed class NewsSnapshot
{
    public static NewsSnapshot Loading { get; } = new() { IsLoading = true };

    public bool IsLoading { get; init; }
    public bool Failed { get; init; }
    public IReadOnlyList<BlogPostItem> Posts { get; init; } = Array.Empty<BlogPostItem>();

    public static NewsSnapshot FromPosts(IReadOnlyList<BlogPostItem> posts) =>
        new() { Posts = posts };

    public static NewsSnapshot Unavailable() =>
        new() { Failed = true };
}
