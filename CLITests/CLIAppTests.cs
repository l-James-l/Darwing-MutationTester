using CLI;
using Core.IndustrialEstate;
using Core.Interfaces;
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

        _cancelationTokenFactory.Generate().Returns(_cancelationToken);

        _app = new CLIApp(_mutationSettings, _statusTracker, _cancelationTokenFactory, _solutionLoader, _solutionBuilder, _mutationRunInitiator);

        //Limit testing to a single run through.
        Queue<bool> ensureSingleRunQueue = new();
        ensureSingleRunQueue.Enqueue(false);
        ensureSingleRunQueue.Enqueue(true);

        _cancelationToken.IsCancelled().Returns(_ => ensureSingleRunQueue.Dequeue());
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
        _app.Run(Array.Empty<string>());

        // Assert
        _solutionLoader.Received(1).Load(Arg.Is<string>(x => x == providedPath));
    }

    [Test]
    public void GivenLaunchSettingPathAndNouserInput_WhenRun_ThenDoesNotPublish()
    {
        // Arrange
        Console.SetIn(new StringReader(Environment.NewLine));

        // Act
        _app.Run(Array.Empty<string>());

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
        _app.Run(Array.Empty<string>());

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
        _app.Run(Array.Empty<string>());

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
        _app.Run(Array.Empty<string>());

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
        _app.Run(Array.Empty<string>());

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
        _app.Run(Array.Empty<string>());

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
        _app.Run(Array.Empty<string>());

        // Assert
        _mutationRunInitiator.DidNotReceive().Run();
    }

}
