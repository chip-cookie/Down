using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace YoutubeDownloader.Features.History;

public class DownloadHistoryService
{
    private readonly string _historyFilePath;
    private readonly object _lock = new();

    public DownloadHistoryService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "YoutubeDownloader"
        );
        Directory.CreateDirectory(appDataPath);
        _historyFilePath = Path.Combine(appDataPath, "download_history.json");
    }

    public void AddEntry(DownloadHistoryEntry entry)
    {
        lock (_lock)
        {
            var entries = LoadEntriesInternal();
            entries.Add(entry);
            SaveEntries(entries);
        }
    }

    public List<DownloadHistoryEntry> GetAllEntries()
    {
        lock (_lock)
        {
            return LoadEntriesInternal();
        }
    }

    public void ClearHistory()
    {
        lock (_lock)
        {
            SaveEntries([]);
        }
    }

    private List<DownloadHistoryEntry> LoadEntriesInternal()
    {
        if (!File.Exists(_historyFilePath))
            return [];

        try
        {
            var json = File.ReadAllText(_historyFilePath);
            return JsonSerializer.Deserialize<List<DownloadHistoryEntry>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void SaveEntries(List<DownloadHistoryEntry> entries)
    {
        var json = JsonSerializer.Serialize(
            entries,
            new JsonSerializerOptions { WriteIndented = true }
        );
        File.WriteAllText(_historyFilePath, json);
    }
}
