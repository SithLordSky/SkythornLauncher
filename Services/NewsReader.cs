using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

internal static class NewsReader
{
    private const int MaxItems = 2;

    private static readonly Regex HtmlTagPattern = new("<[^>]+>", RegexOptions.Compiled);

    public static NewsSnapshot? TryParse(string xml)
    {
        try
        {
            var document = XDocument.Parse(xml);
            var posts = document
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "item", StringComparison.OrdinalIgnoreCase))
                .Select(ParseItem)
                .Where(post => post != null)
                .Cast<BlogPostItem>()
                .Take(MaxItems)
                .ToList();

            if (posts.Count == 0)
            {
                return null;
            }

            return NewsSnapshot.FromPosts(posts);
        }
        catch
        {
            return null;
        }
    }

    private static BlogPostItem? ParseItem(XElement item)
    {
        var title = ReadElementText(item, "title");
        var link = ReadElementText(item, "link");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
        {
            return null;
        }

        var description = ReadElementText(item, "description");
        return new BlogPostItem
        {
            Title = DecodeText(title),
            Excerpt = UiFormat.TrimExcerpt(DecodeText(StripHtml(description))),
            PublishedDate = ReadPubDate(item),
            Url = link.Trim()
        };
    }

    private static string ReadElementText(XElement parent, string localName)
    {
        var element = parent.Elements()
            .FirstOrDefault(node => string.Equals(node.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase));

        return element?.Value?.Trim() ?? string.Empty;
    }

    private static DateTime? ReadPubDate(XElement item)
    {
        var pubDate = ReadElementText(item, "pubDate");
        if (string.IsNullOrWhiteSpace(pubDate))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(pubDate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedOffset))
        {
            return parsedOffset.LocalDateTime;
        }

        return DateTime.TryParse(pubDate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed)
            ? parsed
            : null;
    }

    private static string StripHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return HtmlTagPattern.Replace(value, " ");
    }

    private static string DecodeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return WebUtility.HtmlDecode(value).Trim();
    }
}
