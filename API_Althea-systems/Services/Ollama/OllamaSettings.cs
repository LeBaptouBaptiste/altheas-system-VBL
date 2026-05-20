namespace API_Althea_systems.Services.Ollama;

/// <summary>
/// Bound to the "Ollama" config section. All fields have sensible defaults
/// so a missing section doesn't crash boot — the chatbot just falls back
/// to "service unavailable" replies until a real Ollama is reachable.
/// </summary>
public class OllamaSettings
{
    /// <summary>
    /// Base URL of the Ollama HTTP API. In Docker this is the service name
    /// (<c>http://ollama:11434</c>); for local dev outside Docker it's
    /// <c>http://localhost:11434</c>.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Model tag pulled at runtime by Ollama. Small enough to run on CPU
    /// in reasonable time: <c>llama3.2:3b</c>, <c>qwen2.5:3b</c>,
    /// <c>gemma2:2b</c>. The model is pulled lazily on first call.
    /// </summary>
    public string Model { get; set; } = "llama3.2:3b";

    /// <summary>0–1, higher = more creative. 0.7 is a balanced default.</summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// How long to wait for a single reply before giving up. CPU-only models
    /// can be slow on the first token, so this is generous.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Max number of past turns to send back to the model. Keeps the prompt
    /// from growing unbounded over a long conversation.
    /// </summary>
    public int MaxHistory { get; set; } = 16;

    /// <summary>
    /// System prompt the model is conditioned on for every reply. Defines
    /// the assistant's persona, tone, and refusal policy (e.g. no medical
    /// advice for an e-commerce of medical equipment).
    /// </summary>
    public string SystemPrompt { get; set; } = "You are a helpful assistant.";
}
