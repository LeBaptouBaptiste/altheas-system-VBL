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
    /// Model tag pulled at boot by the ollama-init sidecar. Defaults to
    /// <c>qwen2.5:3b</c> — same family as Qwen3 but NO reasoning mode, so
    /// it doesn't leak English chain-of-thought into the reply when
    /// <c>think:false</c> is ignored by older Ollama builds. ~2 GB on
    /// disk, ~4 GB RAM, ~8 s/reply on CPU. Alternatives:
    /// <c>qwen3:4b</c> (better but emits reasoning unless Ollama
    /// supports the think flag), <c>llama3.2:3b</c>, <c>gemma2:2b</c>,
    /// <c>mistral:7b</c> (best quality, ~8 GB RAM, slower).
    /// </summary>
    public string Model { get; set; } = "qwen2.5:3b";

    /// <summary>0–1, higher = more creative. 0.7 is a balanced default.</summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// How long to wait for a single reply before giving up. CPU-only models
    /// can be slow on the first token, so this is generous.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Max number of past turns to send back to the model. Keeps the prompt
    /// from growing unbounded over a long conversation. Lower = less CPU
    /// per turn but the bot forgets older context faster.
    /// </summary>
    public int MaxHistory { get; set; } = 8;

    /// <summary>
    /// Hard cap on the model's reply length (Ollama's <c>num_predict</c>).
    /// 300 tokens ≈ 4-6 sentences in French — plenty for a customer-support
    /// chatbot, and prevents the model from rambling for 60 s on CPU.
    /// </summary>
    public int MaxOutputTokens { get; set; } = 300;

    /// <summary>
    /// Qwen3 (and a few other reasoning-tuned models) generate a hidden
    /// <c>&lt;think&gt;…&lt;/think&gt;</c> block before the final answer.
    /// Great for math / code, but for a customer chatbot it just triples
    /// the latency. <c>false</c> disables it via the Ollama API's
    /// <c>think</c> flag and a <c>/no_think</c> hint in the system prompt.
    /// No-op on models that don't support reasoning mode.
    /// </summary>
    public bool EnableThinking { get; set; } = false;

    /// <summary>
    /// System prompt the model is conditioned on for every reply. Defines
    /// the assistant's persona, tone, and refusal policy (e.g. no medical
    /// advice for an e-commerce of medical equipment).
    /// </summary>
    public string SystemPrompt { get; set; } = "You are a helpful assistant.";
}
