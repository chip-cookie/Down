using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;

namespace YoutubeDownloader.Features.VideoSummary;

public class WhisperService
{
    private static readonly string ModelsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "YoutubeDownloader",
        "models"
    );

    public WhisperService()
    {
        Directory.CreateDirectory(ModelsDirectory);
    }

    public static string GetModelPath(GgmlType modelType) =>
        Path.Combine(ModelsDirectory, $"ggml-{modelType.ToString().ToLowerInvariant()}.bin");

    public static bool IsModelDownloaded(GgmlType modelType) =>
        File.Exists(GetModelPath(modelType));

    public async Task DownloadModelAsync(
        GgmlType modelType,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var modelPath = GetModelPath(modelType);
        if (File.Exists(modelPath))
            return;

        using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(modelType);
        using var fileStream = File.Create(modelPath);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;

        while ((read = await modelStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            totalRead += read;
            // Note: Cannot report accurate progress without knowing total size
        }
    }

    public async Task<string> TranscribeAsync(
        string audioFilePath,
        GgmlType modelType,
        string language = "auto",
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var modelPath = GetModelPath(modelType);
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Whisper model not found: {modelPath}");

        using var whisperFactory = WhisperFactory.FromPath(modelPath);

        var processorBuilder = whisperFactory.CreateBuilder();

        if (language != "auto")
            processorBuilder.WithLanguage(language);

        using var processor = processorBuilder.Build();

        var segments = new List<string>();
        int segmentCount = 0;

        await foreach (
            var segment in processor.ProcessAsync(File.OpenRead(audioFilePath), cancellationToken)
        )
        {
            segments.Add(segment.Text.Trim());
            segmentCount++;
            progress?.Report(segmentCount);
        }

        return string.Join(" ", segments);
    }
}
