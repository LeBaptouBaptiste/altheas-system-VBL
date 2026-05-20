using System.Net.Http.Json;
using System.Text.Json.Serialization;
using API_Althea_systems.Services.IServices;
using Microsoft.Extensions.Options;

namespace API_Althea_systems.Services.Ollama;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _http;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(
        HttpClient http,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaService> logger)
    {
        _http = http;
        _settings = settings.Value;
        _logger = logger;
        // HttpClient timeout is set at DI time via AddHttpClient — but if the
        // user mutated TimeoutSeconds after that, honor the latest value.
        // (Defensive; in practice the HttpClient lifetime is the same as
        // the OllamaSettings binding.)
        if (_http.Timeout.TotalSeconds < _settings.TimeoutSeconds)
        {
            _http.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }
    }

    public async Task<string> GenerateReplyAsync(
        IReadOnlyList<(string Role, string Content)> history,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(history);

        // Truncate to MaxHistory most recent turns to keep the prompt size
        // bounded. The system prompt sits separately at index 0.
        var trimmed = history.Count <= _settings.MaxHistory
            ? history
            : history.Skip(history.Count - _settings.MaxHistory).ToList();

        var messages = new List<OllamaMessage>(trimmed.Count + 1)
        {
            new("system", _settings.SystemPrompt),
        };
        foreach (var (role, content) in trimmed)
        {
            // Ollama wire vocabulary: "user", "assistant" (or "system").
            // Defensive normalisation in case a caller passes the C# enum
            // name (User / Bot).
            var normalized = role.ToLowerInvariant() switch
            {
                "user" => "user",
                "assistant" or "bot" => "assistant",
                "system" => "system",
                _ => "user",
            };
            messages.Add(new OllamaMessage(normalized, content));
        }

        var request = new OllamaChatRequest(
            Model: _settings.Model,
            Messages: messages,
            Stream: false,
            Options: new OllamaOptions(Temperature: _settings.Temperature));

        try
        {
            using var resp = await _http.PostAsJsonAsync("/api/chat", request, ct);

            // 404 from Ollama almost always means "model not pulled yet"
            // (the daemon itself returns the canonical error body). Surface
            // a typed exception so callers can show a "warming up" message
            // instead of a generic outage.
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var errorBody = await resp.Content.ReadAsStringAsync(ct);
                throw new OllamaModelNotReadyException(_settings.Model, errorBody);
            }

            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Ollama returned a null body.");

            var reply = body.Message?.Content?.Trim();
            if (string.IsNullOrEmpty(reply))
            {
                _logger.LogWarning("Ollama returned an empty reply for model {Model}.", _settings.Model);
                throw new InvalidOperationException("Empty reply from Ollama.");
            }

            _logger.LogInformation(
                "Ollama replied ({ReplyLength} chars, eval={EvalCount} tokens, model={Model})",
                reply.Length, body.EvalCount ?? 0, _settings.Model);
            return reply;
        }
        catch (HttpRequestException ex)
        {
            // Most likely the container is down or the model isn't pulled.
            // Surface a typed-ish error so ChatService can swallow and
            // return a polite fallback to the customer.
            _logger.LogError(ex,
                "Ollama HTTP call failed (BaseUrl={BaseUrl}, Model={Model}).",
                _settings.BaseUrl, _settings.Model);
            throw;
        }
    }

    // ── Wire-format DTOs ─────────────────────────────────
    // System.Text.Json's source-generated mode would be slightly faster,
    // but the chat endpoint is called once per user message — not worth
    // the boilerplate. JsonPropertyName attributes pin the snake_case
    // keys Ollama expects.

    private record OllamaChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<OllamaMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("options")] OllamaOptions Options);

    private record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record OllamaOptions(
        [property: JsonPropertyName("temperature")] double Temperature);

    private record OllamaChatResponse(
        [property: JsonPropertyName("message")] OllamaMessage? Message,
        [property: JsonPropertyName("eval_count")] int? EvalCount);
}

/// <summary>
/// Thrown when Ollama returns 404 — typically "model not pulled yet".
/// Callers (ChatService) catch this specifically to surface a "model is
/// warming up, try again in a minute" message instead of a generic outage.
/// </summary>
public class OllamaModelNotReadyException : Exception
{
    public string Model { get; }
    public OllamaModelNotReadyException(string model, string responseBody)
        : base($"Ollama model '{model}' is not ready: {responseBody}")
    {
        Model = model;
    }
}
