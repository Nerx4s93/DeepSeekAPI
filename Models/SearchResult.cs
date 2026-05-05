namespace DeepSeekAPI.Models;

public class SearchResult
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Snippet { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;

    public string? SiteIcon { get; set; }
    public int? CiteIndex { get; set; }
    public long? PublishedAt { get; set; }
}