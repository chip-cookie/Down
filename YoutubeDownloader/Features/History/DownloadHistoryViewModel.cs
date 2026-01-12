using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using YoutubeDownloader.Framework;

namespace YoutubeDownloader.Features.History;

public partial class DownloadHistoryViewModel : DialogViewModelBase
{
    private readonly DownloadHistoryService _downloadHistoryService;

    public ObservableCollection<DownloadHistoryEntry> Entries { get; } = [];

    public DownloadHistoryViewModel(DownloadHistoryService downloadHistoryService)
    {
        _downloadHistoryService = downloadHistoryService;
        LoadEntries();
    }

    private void LoadEntries()
    {
        Entries.Clear();
        foreach (var entry in _downloadHistoryService.GetAllEntries())
        {
            Entries.Insert(0, entry); // Newest first
        }
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _downloadHistoryService.ClearHistory();
        Entries.Clear();
    }

    [RelayCommand]
    private void CloseDialog() => base.Close(true);
}
