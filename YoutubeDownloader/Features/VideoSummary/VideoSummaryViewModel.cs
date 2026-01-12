using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Whisper.net.Ggml;
using YoutubeDownloader.Features.Settings;
using YoutubeDownloader.Framework;
using YoutubeExplode;
using YoutubeExplode.Videos.ClosedCaptions;

namespace YoutubeDownloader.Features.VideoSummary;

public partial class VideoSummaryViewModel : DialogViewModelBase
{
    private readonly WhisperService _whisperService;
    private readonly AiSummaryService _aiSummaryService;
    private readonly SettingsService _settingsService;

    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    public partial string VideoFilePath { get; set; } = "";

    [ObservableProperty]
    public partial string VideoTitle { get; set; } = "";

    [ObservableProperty]
    public partial string VideoId { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModelReady))]
    public partial GgmlType SelectedModel { get; set; } = GgmlType.Base;

    [ObservableProperty]
    public partial bool IsProcessing { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "준비 중...";

    [ObservableProperty]
    public partial string TranscriptText { get; set; } = "";

    [ObservableProperty]
    public partial string SummaryText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsDownloadingModel { get; set; }

    [ObservableProperty]
    public partial bool UsedYoutubeSubtitles { get; set; }

    public bool IsModelReady => WhisperService.IsModelDownloaded(SelectedModel);

    public bool HasApiKey => !string.IsNullOrWhiteSpace(_settingsService.OpenAiApiKey);

    public GgmlType[] AvailableModels { get; } =
    [GgmlType.Tiny, GgmlType.Base, GgmlType.Small, GgmlType.Medium];

    public VideoSummaryViewModel(
        WhisperService whisperService,
        AiSummaryService aiSummaryService,
        SettingsService settingsService
    )
    {
        _whisperService = whisperService;
        _aiSummaryService = aiSummaryService;
        _settingsService = settingsService;
    }

    [RelayCommand]
    private async Task DownloadModelAsync()
    {
        if (IsDownloadingModel)
            return;

        IsDownloadingModel = true;
        StatusMessage = $"모델 다운로드 중: {SelectedModel}...";

        try
        {
            await _whisperService.DownloadModelAsync(SelectedModel);
            OnPropertyChanged(nameof(IsModelReady));
            StatusMessage = "모델 다운로드 완료!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"모델 다운로드 실패: {ex.Message}";
        }
        finally
        {
            IsDownloadingModel = false;
        }
    }

    [RelayCommand]
    private async Task StartProcessingAsync()
    {
        if (string.IsNullOrWhiteSpace(VideoFilePath) || !File.Exists(VideoFilePath))
        {
            StatusMessage = "유효한 영상 파일을 선택해주세요.";
            return;
        }

        IsProcessing = true;
        UsedYoutubeSubtitles = false;
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        try
        {
            // Step 1: Check for YouTube subtitles first (faster)
            if (!string.IsNullOrWhiteSpace(VideoId))
            {
                StatusMessage = "YouTube 자막 확인 중...";
                try
                {
                    var youtube = new YoutubeClient();
                    var trackManifest = await youtube.Videos.ClosedCaptions.GetManifestAsync(
                        VideoId,
                        token
                    );

                    // Prefer Korean, then English, then any
                    var track = trackManifest
                        .Tracks.OrderByDescending(t => t.Language.Code.StartsWith("ko"))
                        .ThenByDescending(t => t.Language.Code.StartsWith("en"))
                        .FirstOrDefault();

                    if (track is not null)
                    {
                        StatusMessage = $"자막 다운로드 중 ({track.Language.Name})...";
                        var captions = await youtube.Videos.ClosedCaptions.GetAsync(track, token);
                        TranscriptText = string.Join(" ", captions.Captions.Select(c => c.Text));
                        UsedYoutubeSubtitles = true;
                        StatusMessage = "YouTube 자막에서 텍스트 추출 완료!";
                    }
                }
                catch
                {
                    // Subtitles not available - will fallback to Whisper
                }
            }

            // Step 2: Fallback to Whisper STT if no subtitles
            if (string.IsNullOrWhiteSpace(TranscriptText))
            {
                if (!IsModelReady)
                {
                    StatusMessage = "자막이 없습니다. 먼저 Whisper 모델을 다운로드해주세요.";
                    return;
                }

                StatusMessage = "자막 없음. 음성 인식 중... (GPU가 있으면 더 빠릅니다)";
                var progress = new Progress<int>(count =>
                    StatusMessage = $"음성 인식 중... ({count}개 구간 처리됨)"
                );

                TranscriptText = await _whisperService.TranscribeAsync(
                    VideoFilePath,
                    SelectedModel,
                    "auto",
                    progress,
                    token
                );

                StatusMessage = "음성 인식 완료!";
            }

            // Step 3: Summarize (if API key available)
            if (HasApiKey)
            {
                StatusMessage = "AI 요약 생성 중...";
                SummaryText = await _aiSummaryService.SummarizeAsync(
                    TranscriptText,
                    _settingsService.OpenAiApiKey,
                    "ko",
                    token
                );
                StatusMessage = UsedYoutubeSubtitles
                    ? "요약 완료! (YouTube 자막 사용)"
                    : "요약 완료! (Whisper 음성 인식 사용)";
            }
            else
            {
                SummaryText =
                    "(OpenAI API 키가 설정되지 않아 요약을 생성할 수 없습니다. 설정에서 API 키를 입력해주세요.)";
                StatusMessage = UsedYoutubeSubtitles
                    ? "텍스트 추출 완료! (YouTube 자막 사용)"
                    : "텍스트 추출 완료! (Whisper 음성 인식 사용)";
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "작업이 취소되었습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"오류 발생: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelProcessing()
    {
        _cancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    private async Task CopyTranscriptAsync()
    {
        if (
            Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.Clipboard is { } clipboard
        )
            await clipboard.SetTextAsync(TranscriptText);
    }

    [RelayCommand]
    private async Task CopySummaryAsync()
    {
        if (
            Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow?.Clipboard is { } clipboard
        )
            await clipboard.SetTextAsync(SummaryText);
    }

    [RelayCommand]
    private async Task SaveTranscriptAsync()
    {
        if (string.IsNullOrWhiteSpace(TranscriptText))
            return;

        if (
            Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { } window
        )
        {
            var file = await window.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "전체 텍스트(자막) 저장",
                    DefaultExtension = "txt",
                    SuggestedFileName = $"{VideoTitle}_Transcript.txt",
                    FileTypeChoices = [new("Text Files") { Patterns = ["*.txt"] }],
                }
            );

            if (file is not null)
            {
                await using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(TranscriptText);
                StatusMessage = "자막 저장 완료!";
            }
        }
    }

    [RelayCommand]
    private async Task SaveSummaryAsync()
    {
        if (string.IsNullOrWhiteSpace(SummaryText))
            return;

        if (
            Application.Current?.ApplicationLifetime
                is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { } window
        )
        {
            var file = await window.StorageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "AI 요약 저장",
                    DefaultExtension = "txt",
                    SuggestedFileName = $"{VideoTitle}_Summary.txt",
                    FileTypeChoices = [new("Text Files") { Patterns = ["*.txt"] }],
                }
            );

            if (file is not null)
            {
                await using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream);
                await writer.WriteAsync(SummaryText);
                StatusMessage = "요약 저장 완료!";
            }
        }
    }

    [RelayCommand]
    private void CloseDialog() => Close(true);
}
