using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeDownloader.Features.VideoSummary;

public class AiSummaryService
{
    private static readonly HttpClient HttpClient = new();

    public async Task<string> SummarizeAsync(
        string transcript,
        string apiKey,
        string language = "ko",
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("OpenAI API key is required.");

        var systemPrompt = language switch
        {
            "ko" =>
                "당신은 유용한 비서입니다. 주어진 텍스트의 핵심 내용을 3~5개의 글머리 기호로 간결하게 요약해 주세요. 한국어로 응답하세요.",
            "en" =>
                "You are a helpful assistant. Summarize the key points of the given text in 3 to 5 bullet points. Be concise.",
            _ =>
                "You are a helpful assistant. Summarize the key points of the given text in 3 to 5 bullet points.",
        };

        var requestBody = new
        {
            model = "gpt-4o-mini",
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"다음 텍스트를 요약해 주세요:\n\n{transcript}" },
            },
            max_tokens = 1000,
            temperature = 0.5,
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions"
        )
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            ),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);

        var content = doc
            .RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? "요약 내용을 가져올 수 없습니다.";
    }
}
