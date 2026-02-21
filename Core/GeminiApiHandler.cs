using Core.IndustrialEstate;
using Core.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.AI;
using Models;
using Serilog;
using System.Text.Json;

namespace Core;

/// <inheritdoc/>
public sealed class GeminiApiHandler : IDisposable, IGeminiApiHandler
{
    private readonly IChatClient? _client;
    private readonly ISolutionProvider _solutionProvider;
    private readonly IMutationSettings _settings;
    private readonly ICancelationTokenFactory _cancelationTokenFactory;

    public GeminiApiHandler(IGeminiChatClientFactory clientFactory, ISolutionProvider solutionProvider,
        ICancelationTokenFactory cancelationTokenFactory, IMutationSettings settings)
    {
        _client = clientFactory.TryCreate();
        _solutionProvider = solutionProvider;
        _settings = settings;
        _cancelationTokenFactory = cancelationTokenFactory;
    }

    /// <inheritdoc/>
    public bool IsConfigured => _client != null;

    /// <inheritdoc/>
    public async Task GenerateUnitTest(DiscoveredMutation mutation, Action<string, string> callback)
    {
        Log.Information("Gemini API handler invoked.");
        if (_client == null)
        {
            Log.Warning("Tried to invoke Gemini API without a valid client");
            return;
        }

        SourceCodeFileContainer? file = _solutionProvider.SolutionContainer.FindFile(mutation.Document);
        if (file == null)
        {
            Log.Error("Could not find source code file for requested mutation. Unable to provide unit test.");
            return;
        }
        string fullFile = file.SyntaxTree.ToString();
        string originalNode = mutation.OriginalNode.ToFullString();
        string mutatedNode = mutation.MutatedNode.ToFullString();

        GeminiQueryResponse result = await QueryClient(fullFile, originalNode, mutatedNode, mutation.LineSpan.StartLinePosition.Line);
        if (result.Succeeded)
        {
            Log.Debug("Invoking API request callback callback");
            string formattedTest = CSharpSyntaxTree.ParseText(result.NewTestBody).GetRoot().NormalizeWhitespace().ToFullString();
            callback.Invoke(formattedTest, result.TestDescription);
        }
        else
        {
            Log.Debug("API request failed, not invoking callback");
        }
    }

    private async Task<GeminiQueryResponse> QueryClient(string fullFile, string originalNode, string mutatedNode, int lineNo)
    {
        ArgumentNullException.ThrowIfNull(_client);
        try
        {
            Log.Information("Sending API request");
            ChatResponse response = await _client.GetResponseAsync(PromptFormatter(fullFile, originalNode, mutatedNode, lineNo),
            new ChatOptions()
            {
                ResponseFormat = new ChatResponseFormatJson(JsonDocument.Parse(ResponseJsonSchema).RootElement, "Unit test creation schema"),
            },
            _cancelationTokenFactory.Generate().Token);

            Log.Information($"API response received. {response.Text}");

            using var doc = JsonDocument.Parse(response.Text);
            string? code = doc?.RootElement.GetProperty("TestSourceCode").GetString();
            string? desc = doc?.RootElement.GetProperty("Description").GetString();

            if (code is not null && desc is not null)
            {
                return new GeminiQueryResponse
                {
                    Succeeded = true,
                    NewTestBody = code,
                    TestDescription = desc
                };
            }
            else
            {
                Log.Error($"Could not parse Gemini query response, {(code is null ? "test body empty" : "description empty")}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception occurred while getting Gemini query results.");
        }
        return new GeminiQueryResponse()
        {
            Succeeded = false,
            NewTestBody = "No Test Available",
            TestDescription = "Could not generate test."
        };
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private string ResponseJsonSchema = @"
    {
      ""type"": ""object"",
      ""properties"": {
        ""TestSourceCode"": {
          ""type"": ""string"",
          ""description"": ""The C# source code for the new unit test.""
        },
        ""Description"": {
          ""type"": ""string"",
          ""description"": ""Explanation of what the test covers and why it kills the mutation.""
        }
      },
      ""required"": [""TestSourceCode"", ""Description""]
    }";

    private string PromptFormatter(string fullFile, string originalNode, string mutatedNode, int lineNo)
    {
        string basicPrompt = $"You are to suggest a suitable unit test to address a failing mutation from a mutation testing result in the following file. The failed mutation is that line {lineNo} {originalNode} was changed from '{originalNode}' to '{mutatedNode}' and no test failed. Here is the full file: {fullFile}. ";
        basicPrompt += "IMPORTANT: Ensure all source code is formatted properly with correct whitespace and line breaks for best human readability. ";
        if (!string.IsNullOrEmpty(_settings.AiTestGenerationAdditionalInstructions))
        {
            basicPrompt += $"Additional instructions: {_settings.AiTestGenerationAdditionalInstructions}";
        }
        return basicPrompt;
    }

    private struct GeminiQueryResponse
    {
        public bool Succeeded;

        public string NewTestBody;

        public string TestDescription;
    }
}

/// <summary>
/// Data class that the secrets.json file is mapped to so that we can get the api key
/// </summary>
public class GeminiSettings
{
    public string ApiKey { get; set; } = string.Empty;
}