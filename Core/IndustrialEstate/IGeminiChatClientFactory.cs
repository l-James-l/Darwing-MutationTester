using Microsoft.Extensions.AI;

namespace Core.IndustrialEstate;

/// <summary>
/// Factory for a mockable chat client.
/// </summary>
public interface IGeminiChatClientFactory
{
    IChatClient Create();
}