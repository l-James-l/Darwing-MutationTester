using Core;
using Core.IndustrialEstate;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.AI;
using Models;
using Models.Events;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace CoreTests;

public class GeminiApiHandlerTests
{
    private IGeminiChatClientFactory _clientFactory;
    private IChatClient _chatClient;
    private ISolutionProvider _solutionProvider;
    private ICancelationTokenFactory _tokenFactory;
    private IMutationSettings _settings;
    private IEventAggregator _eventAggregator;
    private GeminiApiHandler _sut;

    [SetUp]
    public void SetUp()
    {
        _clientFactory = Substitute.For<IGeminiChatClientFactory>();
        _chatClient = Substitute.For<IChatClient>();
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _tokenFactory = Substitute.For<ICancelationTokenFactory>();
        _settings = Substitute.For<IMutationSettings>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        _eventAggregator.GetEvent<MutationUpdated>().Returns(new MutationUpdated());

        // Ensure the factory returns our mock client
        _clientFactory.TryCreate().Returns(_chatClient);

        // Setup default token
        var cts = Substitute.For<ICancellationTokenWrapper>();
        _tokenFactory.Generate().Returns(cts);

        _sut = new GeminiApiHandler(_clientFactory, _solutionProvider, _tokenFactory, _settings);
    }

    [TearDown]
    public void TearDown()
    {
        _chatClient.Dispose();
        _sut.Dispose();
    }

    [Test]
    public async Task GenerateUnitTest_WhenFileFoundAndApiSucceeds_InvokesCallback()
    {
        // Arrange
        DiscoveredMutation mutation = CreateMutation();
        SetupMockFile(mutation.Document, "public class Math { ... }");

        // Mock a valid JSON response from Gemini
        string jsonResponse = @"{""TestSourceCode"": ""[Fact] public void Test() {}"", ""Description"": ""Test desc""}";
        ChatResponse chatResponse = new(new List<ChatMessage>()
        {
            new (ChatRole.Assistant, jsonResponse)
        });

        _chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
                    .Returns(chatResponse);

        string? receivedCode = null;
        string? receivedDesc = null;

        // Act
        await _sut.GenerateUnitTest(mutation, (code, desc) =>
        {
            receivedCode = code;
            receivedDesc = desc;
        });

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(receivedCode, Is.EqualTo("[Fact]\r\npublic void Test()\r\n{\r\n}"));
            Assert.That(receivedDesc, Is.EqualTo("Test desc"));
        });
    }

    [Test]
    public async Task GenerateUnitTest_WhenFileNotFound_LogsErrorAndDoesNotInvokeCallback()
    {
        // Arrange
        var mutation = CreateMutation();
        _solutionProvider.SolutionContainer.FindFile(Arg.Any<DocumentId>()).ReturnsNull();
        bool callbackInvoked = false;

        // Act
        await _sut.GenerateUnitTest(mutation, (c, d) => callbackInvoked = true);

        // Assert
        Assert.That(callbackInvoked, Is.False);
        await _chatClient.DidNotReceiveWithAnyArgs().GetResponseAsync(default!);
    }

    [Test]
    public async Task GenerateUnitTest_WhenApiReturnsInvalidJson_DoesNotInvokeCallback()
    {
        // Arrange
        DiscoveredMutation mutation = CreateMutation();
        SetupMockFile(mutation.Document, "public void Method1() {}");

        ChatResponse badResponse = new(new ChatMessage(ChatRole.Assistant, "Not JSON at all"));

        _chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
                    .Returns(badResponse);

        bool callbackInvoked = false;

        // Act
        await _sut.GenerateUnitTest(mutation, (c, d) => callbackInvoked = true);

        // Assert
        Assert.That(callbackInvoked, Is.False);
    }

    [Test]
    public async Task GenerateUnitTest_IncludesAdditionalInstructionsInPrompt()
    {
        // Arrange
        DiscoveredMutation mutation = CreateMutation();
        SetupMockFile(mutation.Document, "content");
        
        _settings.AiTestGenerationAdditionalInstructions = "Use FluentAssertions.";

        string jsonResponse = @"{""TestSourceCode"": ""[Fact] public void Test() {}"", ""Description"": ""Test desc""}";
        ChatResponse chatResponse = new(new ChatMessage(ChatRole.Assistant, jsonResponse));
        _chatClient.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
                    .Returns(chatResponse);

        // Act
        _sut.GenerateUnitTest(mutation, (c, d) => { }).GetAwaiter().GetResult();

        // Assert
        await _chatClient.Received().GetResponseAsync(
            Arg.Is<IEnumerable<ChatMessage>>(x => x.Count() == 1 && x.First().Text.Contains("Additional instructions: Use FluentAssertions.")),
            Arg.Any<ChatOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public void GivenNoClient_WhenCheckingIsConfigured_ThenReturnsFalse()
    {
        // Arrange
        _clientFactory.TryCreate().ReturnsNull();
        var handler = new GeminiApiHandler(_clientFactory, _solutionProvider, _tokenFactory, _settings);

        // Act
        bool isConfigured = handler.IsConfigured;

        // Assert
        Assert.That(isConfigured, Is.False);
    }

    [Test]
    public void GivenNullClient_WhenGeneratingUnitTest_ThenLogsWarningAndDoesNotThrow()
    {
        // Arrange
        _clientFactory.TryCreate().ReturnsNull();
        var handler = new GeminiApiHandler(_clientFactory, _solutionProvider, _tokenFactory, _settings);
        var mutation = CreateMutation();
        bool callbackInvoked = false;
        // Act & Assert
        Assert.DoesNotThrowAsync(() => handler.GenerateUnitTest(mutation, (c, d) => callbackInvoked = true));
        Assert.That(callbackInvoked, Is.False);
    }

    private void SetupMockFile(DocumentId docId, string content)
    {
        SourceCodeFileContainer file = new(docId, CSharpSyntaxTree.ParseText(content));

        _solutionProvider.SolutionContainer.FindFile(docId).Returns(file);
    }

    private DiscoveredMutation CreateMutation()
    {
        return new DiscoveredMutation(new SyntaxAnnotation(), CSharpSyntaxTree.ParseText("a == b").GetRoot(),
             SyntaxFactory.EmptyStatement(), CSharpSyntaxTree.ParseText("a != b").GetRoot(), _eventAggregator, 0, 0)
        {
            Document = DocumentId.CreateNewId(ProjectId.CreateNewId()),
            LineSpan = new FileLinePositionSpan("test.cs", new LinePosition(26, 0), new LinePosition(26, 10))
        };
    }
}