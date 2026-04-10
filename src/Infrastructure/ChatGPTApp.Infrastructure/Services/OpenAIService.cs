using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ChatGPTApp.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChatGPTApp.Infrastructure.Services;

public class OpenAIService : IOpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl = "https://api.openai.com/v1";

    public OpenAIService(HttpClient httpClient, IConfiguration config, ILogger<OpenAIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<(string Reply, int TokensUsed)> GetChatCompletionAsync(
        string model,
        List<(string Role, string Content)> messages,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            max_tokens = 2000,
            temperature = 0.7
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/chat/completions", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OpenAI API error: {StatusCode} - {Body}", response.StatusCode, errorBody);
            throw new Exception($"OpenAI API error: {response.StatusCode}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        var reply = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";

        var tokensUsed = root
            .GetProperty("usage")
            .GetProperty("total_tokens")
            .GetInt32();

        return (reply.Trim(), tokensUsed);
    }
}
