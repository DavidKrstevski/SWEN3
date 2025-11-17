using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenAIWorker;

public class GeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiClient> _logger;
    private readonly string _model;
    private readonly string _apiKey;

    public GeminiClient(HttpClient httpClient, IConfiguration config, ILogger<GeminiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _model = config["GEMINI_MODEL"] ?? "gemini-2.0-flash";
        _apiKey = config["GEMINI_API_KEY"] ?? throw new InvalidOperationException("GEMINI_API_KEY not set");

        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
    }

    public async Task<string?> SummarizeAsync(string text, CancellationToken ct = default)
    {
        var prompt = $"Fasse den folgenden Text kurz zusammen: \n{text}";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var url = $"models/{_model}:generateContent";
            _logger.LogInformation("Calling Gemini model {Model}", _model);

            using var response = await _httpClient.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Gemini API error {StatusCode}: {Error}",
                    response.StatusCode, errorText);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            return ExtractText(responseJson);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling Gemini");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Gemini request timed out");
            return null;
        }
    }

    private static string? ExtractText(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array ||
            candidates.GetArrayLength() == 0)
            return null;

        var firstCandidate = candidates[0];
        if (!firstCandidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.ValueKind != JsonValueKind.Array ||
            parts.GetArrayLength() == 0)
            return null;

        var firstPart = parts[0];
        if (!firstPart.TryGetProperty("text", out var textElement))
            return null;

        return textElement.GetString();
    }
}
