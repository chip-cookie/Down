using System;
using System.Text.Json.Serialization;

namespace YoutubeDownloader.Features.History;

public class DownloadHistoryEntry
{
    [JsonPropertyName("videoId")]
    public string VideoId { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("downloadedAt")]
    public DateTimeOffset DownloadedAt { get; set; }

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = "";

    [JsonPropertyName("format")]
    public string Format { get; set; } = "";
}
