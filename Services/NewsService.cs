using System.Net.Http;
using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

internal sealed class NewsService : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(4) };

    public event Action<NewsSnapshot>? NewsUpdated;

    public NewsSnapshot Latest { get; private set; } = NewsSnapshot.Loading;

    public async Task RefreshAsync()
    {
        NewsUpdated?.Invoke(NewsSnapshot.Loading);

        var snapshot = await QueryAsync();
        Latest = snapshot;
        NewsUpdated?.Invoke(snapshot);
    }

    private async Task<NewsSnapshot> QueryAsync()
    {
        if (string.IsNullOrWhiteSpace(LauncherConstants.NewsFeedUrl))
        {
            return NewsSnapshot.Unavailable();
        }

        try
        {
            var xml = await _http.GetStringAsync(LauncherConstants.NewsFeedUrl);
            var snapshot = NewsReader.TryParse(xml);
            if (snapshot != null && snapshot.Posts.Count > 0)
            {
                return snapshot;
            }
        }
        catch
        {
            // Keep launcher usable when the endpoint is down.
        }

        return NewsSnapshot.Unavailable();
    }

    public void Dispose() => _http.Dispose();
}
