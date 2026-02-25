using Models;

namespace Core.Interfaces;

/// <summary>
/// Handles API interaction with the gemini API
/// 
/// To use this API, you must set up the API key in your user secrets.
/// To create an API key go to https://aistudio.google.com/app/api-keys
/// Once you have created an API key, use the command: dotnet user-secrets set "Gemini:ApiKey" "YOUR KEY"
/// </summary>
public interface IGeminiApiHandler
{
    /// <summary>
    /// Indicates whether a client was able to be created 
    /// </summary>
    public bool IsConfigured { get; }

    /// <summary>
    /// Ask Gemini for a unit test to address the surviving mutation
    /// async
    /// </summary>
    /// <param name="mutation">The mutation in need of a test</param>
    /// <param name="callback">Method to invoke in the event of a successful API response. 
    /// first param is the new test code, second is a description of the new test</param>
    Task GenerateUnitTest(DiscoveredMutation mutation, Action<string, string> callback);
}