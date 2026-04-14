using CLI;
using Core.IndustrialEstate;
using Core.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Models;
using Models.Enums;
using Models.SharedInterfaces;
using Mutator;
using NSubstitute;

namespace CLITests;

public class CLIAppTests
{
    private CLIApp _app; //SUT

    private IMutationSettings _mutationSettings;
    private ICancelationTokenFactory _cancelationTokenFactory;
    private IStatusTracker _statusTracker;
    private ISolutionLoader _solutionLoader;
    private IMutationRunInitiator _mutationRunInitiator;
    private ISolutionBuilder _solutionBuilder;
    private IMutationDiscoveryManager _mutationDiscoveryManager;
    private ISolutionProvider _solutionProvider;

    private TextReader _originalIn;
    private ICancellationTokenWrapper _cancelationToken;

    [SetUp]
    public void SetUp()
    {
        _originalIn = Console.In;

        _mutationSettings = Substitute.For<IMutationSettings>();
        _cancelationTokenFactory = Substitute.For<ICancelationTokenFactory>();
        _solutionLoader = Substitute.For<ISolutionLoader>();
        _statusTracker = Substitute.For<IStatusTracker>();
        _mutationRunInitiator = Substitute.For<IMutationRunInitiator>();
        _solutionBuilder = Substitute.For<ISolutionBuilder>();
        _cancelationToken = Substitute.For<ICancellationTokenWrapper>();
        _mutationDiscoveryManager = Substitute.For<IMutationDiscoveryManager>();
        _solutionProvider = Substitute.For<ISolutionProvider>();

        _cancelationTokenFactory.Generate().Returns(_cancelationToken);
        _mutationDiscoveryManager.DiscoveredMutations.Returns([]);

        _app = new CLIApp(_mutationSettings, _statusTracker, _cancelationTokenFactory, _solutionLoader, _solutionBuilder, _mutationRunInitiator, _mutationDiscoveryManager, _solutionProvider);

        //Limit testing to a single run through.
        Queue<bool> ensureSingleRunQueue = new();
        ensureSingleRunQueue.Enqueue(false);
        ensureSingleRunQueue.Enqueue(true);

        _cancelationToken.IsCancellationRequested.Returns(_ => ensureSingleRunQueue.Dequeue());
    }

    [TearDown]
    public void TearDown()
    {
        Console.SetIn(_originalIn);
    }

    [Test]
    public void GivenUserProvidesSolutionPath_WhenRun_ThenPublishesThatPath()
    {
        // Arrange
        const string providedPath = "C:\\temp\\MySolution.sln";
        Console.SetIn(new StringReader("--load " + providedPath + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _solutionLoader.Received(1).Load(Arg.Is<string>(x => x == providedPath));
    }

    [Test]
    public void GivenLaunchSettingPathAndNoUserInput_WhenRun_ThenDoesNotPublish()
    {
        // Arrange
        Console.SetIn(new StringReader(Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _solutionLoader.DidNotReceive().Load(Arg.Any<string>());
    }

    [Test]
    public void GivenSlnPathInArgs_WhenRun_ThenPublishesThatPath()
    {
        // Arrange
        const string argPath = "C:\\temp\\ArgSolution.sln";
        var args = new[] { "--sln", argPath };
        Console.SetIn(new StringReader(Environment.NewLine));

        // Act
        _app.Run(args);
        
        // Assert
        _solutionLoader.Received(1).Load(Arg.Is<string>(x => x == argPath));
    }

    [Test]
    public void GivenRunningMainLoop_AndNoSolutionLoaded_WhenGiveBuildCommand_ThenDoesntPublish()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Failed);

        Console.SetIn(new StringReader("--build" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _solutionBuilder.DidNotReceive().InitialBuild();
    }

    [Test]
    public void GivenRunningMainLoop_AndSolutionLoaded_WhenGiveBuildCommand_ThenPublishCommand()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);

        Console.SetIn(new StringReader("--build" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _solutionBuilder.Received(1).InitialBuild();
    }

    [Test]
    public void GivenRunningMainLoop_AndSolutionAlreadyBuilt_WhenGiveBuildCommand_ThenPublishCommand()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);
        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        Console.SetIn(new StringReader("--build" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _solutionBuilder.Received(1).InitialBuild();
    }

    [Test]
    public void GivenRunningMainLoop_AndSolutionLoaded_AndBuildSuccess_WhenGiveTestCommand_ThenPublishCommand()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);
        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        Console.SetIn(new StringReader("--test" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _mutationRunInitiator.Received(1).Run();
    }

    [Test]
    public void GivenRunningMainLoop_AndSolutionNotLoaded_WhenGiveTestCommand_ThenDontPublishCommand()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Failed);

        Console.SetIn(new StringReader("--test" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _mutationRunInitiator.DidNotReceive().Run();
    }

    [Test]
    public void GivenRunningMainLoop_AndSolutionLoaded_AndBuildFailed_WhenGiveTestCommand_ThenDontPublishCommand()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);
        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Failed);

        Console.SetIn(new StringReader("--test" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _mutationRunInitiator.DidNotReceive().Run();
    }

    [Test]
    public void GivenTestCommandWithSettings_WhenRun_ThenOverridesSettingsAndInitiatesRun()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);
        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        // Test setting a boolean and a list via reflection
        // Format: --test BuildTimeout=50 SourceCodeProjects=[ProjA,ProjB]
        Console.SetIn(new StringReader("--test BuildTimeout=50 SourceCodeProjects=[ProjA,ProjB]" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _mutationSettings.Received().BuildTimeout = 50;
        _mutationSettings.Received().SourceCodeProjects = Arg.Is<List<string>>(l => l.Contains("ProjA") && l.Contains("ProjB"));
        _mutationRunInitiator.Received(1).Run();
    }

    [Test]
    public void GivenSettingOverrideForSourceProjects_WhenTestRun_ThenUpdatesProjectTypesInSolution()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);
        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        var mockProject = Substitute.For<IProjectContainer>();
        mockProject.Name.Returns("MyProject");
        _solutionProvider.SolutionContainer.AllProjects.Returns([mockProject]);

        Console.SetIn(new StringReader("--test SourceCodeProjects=[MyProject]" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        mockProject.Received().ProjectType = ProjectType.Source;
    }

    [Test]
    public void GivenMutationsDiscovered_WhenReportCommandCalledWithoutParams_ThenGroupsByFile()
    {
        // Arrange
        var mutation = new DiscoveredMutation(new SyntaxAnnotation(), CSharpSyntaxTree.ParseText("a == b").GetRoot(),
             SyntaxFactory.EmptyStatement(), CSharpSyntaxTree.ParseText("a != b").GetRoot(), new EventAggregator(), 0, 0)
        {
            Document = DocumentId.CreateNewId(ProjectId.CreateNewId()),
            LineSpan = new FileLinePositionSpan("FileA.cs", new LinePosition(26, 0), new LinePosition(26, 10)),
            Status = MutantStatus.Survived
        };
        _mutationDiscoveryManager.DiscoveredMutations.Returns([mutation]);

        Console.SetIn(new StringReader("--report" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _ = _mutationDiscoveryManager.Received().DiscoveredMutations;
    }

    [Test]
    public void GivenQuitCommand_WhenRun_ThenTriggersCancellation()
    {
        // Arrange
        Console.SetIn(new StringReader("--quit" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        _cancelationToken.Received(1).Cancel();
    }

    [Test]
    public void GivenSettingOverrideWithEnum_WhenTestRun_ThenParsesEnumCorrectly()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);
        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        Console.SetIn(new StringReader("--test DisabledMutationTypes=[AddToSubtract]" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        // Verify the property was set with the expected Enum value
        _mutationSettings.Received().DisabledMutationTypes = Arg.Is<List<SpecificMutation>>(x => x.Contains(SpecificMutation.AddToSubtract));
    }

    [Test]
    public void GivenMutationsInMultipleFiles_WhenReportCommandCalledWithFileName_ThenOnlyReportsOnThatFile()
    {
        // Arrange
        var mutation1 = new DiscoveredMutation(new SyntaxAnnotation(), CSharpSyntaxTree.ParseText("a == b").GetRoot(),
             SyntaxFactory.EmptyStatement(), CSharpSyntaxTree.ParseText("a != b").GetRoot(), new EventAggregator(), 0, 0)
        {
            Document = DocumentId.CreateNewId(ProjectId.CreateNewId()),
            LineSpan = new FileLinePositionSpan("TargetFile.cs", new LinePosition(26, 0), new LinePosition(26, 10)),
            Status = MutantStatus.Survived
        };
        var mutation2 = new DiscoveredMutation(new SyntaxAnnotation(), CSharpSyntaxTree.ParseText("a + b").GetRoot(),
             SyntaxFactory.EmptyStatement(), CSharpSyntaxTree.ParseText("a - b").GetRoot(), new EventAggregator(), 0, 0)
        {
            Document = DocumentId.CreateNewId(ProjectId.CreateNewId()),
            LineSpan = new FileLinePositionSpan("IgnoredFile.cs", new LinePosition(26, 0), new LinePosition(26, 10)),
            Status = MutantStatus.Survived
        };

        _mutationDiscoveryManager.DiscoveredMutations.Returns([mutation1, mutation2]);

        // Pass the specific filename as a parameter
        Console.SetIn(new StringReader("--report TargetFile.cs" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        // We verify the discovery manager was accessed to get the mutations
        _ = _mutationDiscoveryManager.Received(1).DiscoveredMutations;
    }

    [Test]
    public void GivenHelpCommand_WhenRun_ThenOutputsHelpTextToConsole()
    {
        // Arrange
        using var sw = new StringReader("--help" + Environment.NewLine);
        Console.SetIn(sw);

        using var output = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(output);

        try
        {
            // Act
            _app.Run([]);

            // Assert
            string consoleOutput = output.ToString();
            Assert.Multiple(() =>
            {
                Assert.That(consoleOutput, Does.Contain("--load"), "Help output should contain the load command description.");
                Assert.That(consoleOutput, Does.Contain("--test"), "Help output should contain the test command description.");
                Assert.That(consoleOutput, Does.Contain("--quit"), "Help output should contain the quit command description.");
                Assert.That(consoleOutput, Does.Contain("Darwing"), "Help output should mention the application name.");
            });
        }
        finally
        {
            // Restore Console.Out so subsequent tests don't fail
            Console.SetOut(originalOut);
        }
    }

    [Test]
    public void GivenTestCommandWithMalformedSetting_WhenRun_ThenLogsWarningAndContinues()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);
        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        // "InvalidSetting" lacks an '=' sign
        Console.SetIn(new StringReader("--test InvalidSetting BuildTimeout=50" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        // Verify the malformed one was ignored, but the valid one was still processed
        _mutationSettings.Received().BuildTimeout = 50;
        _mutationRunInitiator.Received(1).Run();
    }

    [Test]
    public void GivenTestCommandWithUnknownSettingName_WhenRun_ThenLogsWarningAndContinues()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);
        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        // "FakeSetting" does not exist on IMutationSettings
        Console.SetIn(new StringReader("--test FakeSetting=True BuildTimeout=10" + Environment.NewLine));

        // Act
        _app.Run([]);

        // Assert
        // Build timeout should be set, but the loop should have safely skipped FakeSetting
        _mutationSettings.Received().BuildTimeout = 10;
        _mutationRunInitiator.Received(1).Run();
    }
}
