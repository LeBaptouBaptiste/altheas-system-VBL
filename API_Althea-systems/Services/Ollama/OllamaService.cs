using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using API_Althea_systems.Services.IServices;
using Microsoft.Extensions.Options;

namespace API_Althea_systems.Services.Ollama;

public class OllamaService : IOllamaService
{
    // Matches `<think>…</think>` blocks Qwen3 / DeepSeek-R1 emit when
    // their reasoning mode leaks through despite our disables. Compiled
    // once at startup, applied to every reply as a safety net.
    private static readonly Regex ThinkBlockRegex =
        new(@"<think>.*?</think>", RegexOptions.Compiled | RegexOptions.Singleline);

    // English chain-of-thought preamble markers. Models that reason in
    // English (Qwen3, DeepSeek-R1) often start their reply with one of
    // these even with `think:false`. If a response starts with one, we
    // strip everything until we find genuine French content.
    private static readonly string[] EnglishCotMarkers =
    [
        "okay,", "okay.", "okay ", "okay\n",
        "alright,", "alright.", "alright ",
        "let me", "let's ",
        "first,", "first.", "first ", "firstly,",
        "the user", "the customer",
        "i need to", "i should", "i will", "i'll ",
        "now,", "now i ",
        "looking at", "based on the context",
        "to answer", "to respond",
    ];

    // Strongly French content marker: any accented Latin char or a French
    // function word. Used to find the boundary between the English
    // reasoning preamble and the actual French answer.
    private static readonly Regex FrenchMarkerRegex =
        new(@"[àâäçéèêëîïôöùûüÿœæÀÂÄÇÉÈÊËÎÏÔÖÙÛÜŸŒÆ]|\b(le|la|les|un|une|des|du|nous|vous|votre|notre|monsieur|madame|bonjour|merci)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
            // Qwen3's reasoning mode is OFF for chatbot use — see EnableThinking
            // doc on OllamaSettings. No-op on non-reasoning models, the field
            // is just ignored by their handlers.
            Think: _settings.EnableThinking,
            Options: new OllamaOptions(
                Temperature: _settings.Temperature,
                // Hard cap on generated tokens. The customer chatbot answers
                // in 3-5 sentences per the system prompt — 300 tokens is a
                // comfortable upper bound. Stops the model from running for
                // 60 s on CPU when it could've finished in 5.
                NumPredict: _settings.MaxOutputTokens));

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

            var raw = body.Message?.Content?.Trim() ?? string.Empty;
            // Strip <think>…</think> blocks (Qwen3 / R1 when reasoning leaks
            // through despite think:false + /no_think).
            var stripped = ThinkBlockRegex.Replace(raw, "").Trim();
            // Detect + strip a leaked English chain-of-thought preamble.
            // Some reasoning-trained models emit their CoT in English as
            // plain content (no tags) before switching to the French
            // answer — we cut everything up to the first French marker.
            var reply = StripEnglishPreamble(stripped);

            if (string.IsNullOrEmpty(reply))
            {
                _logger.LogWarning(
                    "Ollama returned an empty/unusable reply (raw length {Length}) for model {Model}.",
                    raw.Length, _settings.Model);
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

    /// <summary>
    /// If the reply starts with telltale English chain-of-thought markers
    /// ("Okay,", "Let me", "The user"…), scan forward to the first French
    /// content marker (accented char or function word) and return only
    /// that part onward. If no French is found, returns empty so the
    /// caller surfaces the fallback message instead of broken English.
    ///
    /// This is intentionally aggressive — false positives on edge cases
    /// (a French reply that happens to start with "Okay") are acceptable
    /// because the next attempt will produce a clean answer, whereas
    /// shipping English CoT to the customer never is.
    /// </summary>
    private static string StripEnglishPreamble(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var head = text.TrimStart();
        var lower = head.ToLowerInvariant();

        var startsWithCot = EnglishCotMarkers.Any(m => lower.StartsWith(m));
        if (!startsWithCot)
        {
            return head;
        }

        // Find the first French content marker.
        var match = FrenchMarkerRegex.Match(head);
        if (!match.Success) return string.Empty;

        // Try to back up to the previous sentence terminator so we open
        // on a clean sentence boundary instead of mid-clause.
        var sentenceStart = -1;
        for (var i = match.Index - 1; i >= 0; i--)
        {
            if (head[i] is '.' or '!' or '?' or '\n')
            {
                sentenceStart = i + 1;
                break;
            }
        }

        int start;
        if (sentenceStart >= 0)
        {
            // Found a previous terminator → the French answer starts a
            // fresh sentence after the English CoT. Easy case.
            start = sentenceStart;
        }
        else
        {
            // No terminator between the CoT marker and the French content
            // (rare: model went "Okay, voici…" without an English sentence).
            // Strip just the CoT marker prefix so we don't ship "okay,".
            var marker = EnglishCotMarkers.FirstOrDefault(m => lower.StartsWith(m));
            start = marker?.Length ?? 0;
        }

        return head[start..].TrimStart(' ', '\t', '\r', '\n', '.', ',', ':').Trim();
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
        // Top-level "think" toggles Qwen3 / DeepSeek-R1 reasoning mode.
        // Older Ollama versions silently ignore the field.
        [property: JsonPropertyName("think")] bool Think,
        [property: JsonPropertyName("options")] OllamaOptions Options);

    private record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record OllamaOptions(
        [property: JsonPropertyName("temperature")] double Temperature,
        // num_predict = -1 means "unlimited"; we always set a positive cap
        // so the model can't ramble past our token budget.
        [property: JsonPropertyName("num_predict")] int NumPredict);

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
