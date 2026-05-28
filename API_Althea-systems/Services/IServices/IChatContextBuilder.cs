namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Assembles a "context block" that's prepended to every chat turn so the
/// LLM can answer concretely instead of hallucinating: who the customer
/// is, what they've ordered recently, what catalog we carry, what
/// products match their question.
///
/// One method on purpose — the chatbot is the only consumer, and a single
/// method keeps the surface tiny for mocking in tests.
/// </summary>
public interface IChatContextBuilder
{
    /// <summary>
    /// Returns a plain-text block summarising the customer + likely-relevant
    /// catalog items. Inserted as an additional "system" message right
    /// before the user's question. Safe to send to a local LLM — all data
    /// here is the customer's own + the public catalog, no third-party
    /// PII leak.
    /// </summary>
    /// <param name="locale">
    /// Front-end UI language (<c>fr</c> / <c>en</c> / <c>ms</c> / <c>ar</c>).
    /// Used to instruct the model to reply in that language. Null / unknown
    /// values fall back to French (the historic default).
    /// </param>
    Task<string> BuildAsync(
        Guid? userId,
        string userMessage,
        string? locale = null,
        CancellationToken ct = default);
}
