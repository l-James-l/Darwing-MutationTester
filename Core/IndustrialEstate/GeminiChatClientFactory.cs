using Google.GenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Serilog;

namespace Core.IndustrialEstate;

/// <inheritdoc/>
public class GeminiChatClientFactory : IGeminiChatClientFactory
{
    private readonly string _key;

    public GeminiChatClientFactory(IOptions<GeminiSettings> key)
    {
        _key = key.Value.ApiKey;
    }

    /// <summary>
    /// Returns the gemini-2.5-flash model because its fast
    /// </summary>
    public IChatClient? TryCreate()
    {
        try
        {
            return new Client(apiKey: _key).AsIChatClient("gemini-2.5-flash");
        }
        catch
        {
            Log.Error("Failed to create gemini API");
            return null;
        }
    }
}
