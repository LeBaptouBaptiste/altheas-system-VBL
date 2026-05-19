using System.Net.Http.Json;
using System.Text.Json.Serialization;
using API_Althea_systems.Common.Exceptions;
using API_Althea_systems.Models.Messaging;
using API_Althea_systems.Repositories.IRepositories;
using API_Althea_systems.Services.IServices;

namespace API_Althea_systems.Services;

public class OllamaChatAssistantService : IChatAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly IMessageRepository _messageRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaChatAssistantService> _logger;

    public OllamaChatAssistantService(
        HttpClient httpClient,
        IMessageRepository messageRepository,
        IConfiguration configuration,
        ILogger<OllamaChatAssistantService> logger)
    {
        _httpClient = httpClient;
        _messageRepository = messageRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ChatMessageDto> GenerateReplyAsync(Guid conversationId, ChatMessageCreateRequest request)
    {
        _ = await _messageRepository.GetConversationByIdAsync(conversationId)
            ?? throw new NotFoundException("ChatConversation", conversationId);

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = ChatRole.User,
            Content = request.Content
        };

        await _messageRepository.AddChatMessageAsync(userMessage);

        var updatedConversation = await _messageRepository.GetConversationByIdAsync(conversationId)
            ?? throw new NotFoundException("ChatConversation", conversationId);

        try
        {
            var replyText = await GenerateAssistantReplyAsync(updatedConversation, request.Content);

            var assistantMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Role = ChatRole.Bot,
                Content = replyText
            };

            await _messageRepository.AddChatMessageAsync(assistantMessage);
            return new ChatMessageDto(assistantMessage.Id, assistantMessage.Role, assistantMessage.Content, assistantMessage.Timestamp);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Ollama is unavailable, returning fallback response for conversation {ConversationId}", conversationId);

            var fallbackMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Role = ChatRole.Bot,
                Content = "Le moteur IA est temporairement indisponible. Merci de reessayer dans un instant ou de contacter le support."
            };

            await _messageRepository.AddChatMessageAsync(fallbackMessage);
            return new ChatMessageDto(fallbackMessage.Id, fallbackMessage.Role, fallbackMessage.Content, fallbackMessage.Timestamp);
        }
    }

    private async Task<string> GenerateAssistantReplyAsync(ChatConversation conversation, string userContent)
    {
        var model = _configuration["Ollama:Model"] ?? "qwen2.5:3b";
        var systemPrompt = _configuration["Ollama:SystemPrompt"]
            ?? "You are Althea Systems' customer support assistant. Reply in the same language as the user. If you are unsure, say you do not know and suggest support.";

        var messages = new List<OllamaMessage>
        {
            new("system", systemPrompt)
        };

        messages.AddRange(conversation.Messages
            .OrderBy(message => message.Timestamp)
            .Select(MapToOllamaMessage));

        var request = new OllamaChatRequest(
            model,
            messages,
            false,
            new OllamaOptions(
                NumPredict: GetIntSetting("Ollama:MaxTokens", 512),
                NumCtx: GetIntSetting("Ollama:NumCtx", 1024),
                Temperature: GetDoubleSetting("Ollama:Temperature", 0.2)));

        var response = await _httpClient.PostAsJsonAsync("api/chat", request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
        var reply = payload?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new InvalidOperationException("Ollama returned an empty assistant response.");
        }

        return reply;
    }

    private static OllamaMessage MapToOllamaMessage(ChatMessage message) => message.Role switch
    {
        ChatRole.User => new OllamaMessage("user", message.Content),
        ChatRole.Bot => new OllamaMessage("assistant", message.Content),
        _ => new OllamaMessage("user", message.Content)
    };

    private int GetIntSetting(string key, int fallback)
        => int.TryParse(_configuration[key], out var value) ? value : fallback;

    private double GetDoubleSetting(string key, double fallback)
        => double.TryParse(_configuration[key], out var value) ? value : fallback;

    private sealed record OllamaChatRequest(
        string Model,
        IReadOnlyList<OllamaMessage> Messages,
        bool Stream,
        OllamaOptions Options);

    private sealed record OllamaMessage(string Role, string Content);

    private sealed record OllamaOptions(
        [property: JsonPropertyName("num_predict")] int NumPredict,
        [property: JsonPropertyName("num_ctx")] int NumCtx,
        double Temperature);

    private sealed record OllamaChatResponse(
        [property: JsonPropertyName("message")] OllamaChatResponseMessage? Message);

    private sealed record OllamaChatResponseMessage(string Role, string Content);
}