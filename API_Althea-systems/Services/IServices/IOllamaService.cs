namespace API_Althea_systems.Services.IServices;

/// <summary>
/// Thin HTTP wrapper over the Ollama API (https://github.com/ollama/ollama).
/// Used by <see cref="ChatService"/> to turn a customer message + the
/// running conversation history into an assistant reply.
///
/// One method on purpose: the chatbot is the only consumer right now, and
/// keeping the surface tiny means it's easy to mock in tests.
/// </summary>
public interface IOllamaService
{
    /// <summary>
    /// Sends the running conversation to Ollama and returns the assistant's
    /// reply. The implementation prepends the configured system prompt and
    /// trims the history to the configured MaxHistory window so a long
    /// thread doesn't blow the model's context budget.
    /// </summary>
    /// <param name="history">
    /// Past turns, oldest first. Each tuple is (role, content) where role
    /// is "user" or "assistant" (matching the Ollama wire format).
    /// </param>
    /// <returns>The assistant reply, plain text.</returns>
    Task<string> GenerateReplyAsync(
        IReadOnlyList<(string Role, string Content)> history,
        CancellationToken ct = default);
}
