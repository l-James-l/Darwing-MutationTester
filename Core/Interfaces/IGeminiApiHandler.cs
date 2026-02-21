using Models;

namespace Core.Interfaces;

/// <summary>
/// Handles API interaction with the gemini API
/// </summary>
public interface IGeminiApiHandler
{
    /// <summary>
    /// Ask Gemini for a unit test to address the surviving mutation
    /// async
    /// </summary>
    /// <param name="mutation">The mutation in need of a test</param>
    /// <param name="callback">Method to invoke in the event of a successful API response. 
    /// first param is the new test code, second is a description of the new test</param>
    Task GenerateUnitTest(DiscoveredMutation mutation, Action<string, string> callback);
}