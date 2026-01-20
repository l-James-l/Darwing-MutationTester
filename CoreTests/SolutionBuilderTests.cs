using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Enums;
using Models.SharedInterfaces;
using NSubstitute;
using System.Diagnostics;

namespace CoreTests;

public class SolutionBuilderTests
{
    private SolutionBuilder _projectBuilder; //SUT

    private IMutationSettings _mutationSettings;
    private ISolutionProvider _solutionProvider;
    private IProcessWrapperFactory _processWrapperFactory;
    private IStatusTracker _statusTracker;

    [SetUp]
    public void SetUp()
    {
        _mutationSettings = Substitute.For<IMutationSettings>();
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _processWrapperFactory = Substitute.For<IProcessWrapperFactory>();
        _statusTracker = Substitute.For<IStatusTracker>();

        _projectBuilder = new SolutionBuilder(_mutationSettings, _solutionProvider, _processWrapperFactory, _statusTracker);
    }

    [Test]
    public void GivenNoSolutionLoaded_WhenBuildSolution_ThenNoProcessesStarted()
    {
        //Arrange
        _solutionProvider.IsAvailable.Returns(false);
        _statusTracker.TryStartOperation(DarwingOperation.BuildSolution).Returns(true);

        //Act
        _projectBuilder.InitialBuild();

        //Assert
        _processWrapperFactory.Received(0).Create(Arg.Any<ProcessStartInfo>());
    }

    [Test]
    public void GivenStatusTrackerSaysNo_WhenBuildSolution_ThenNoProcessesStarted()
    {
        //Arrange
        _statusTracker.TryStartOperation(DarwingOperation.BuildSolution).Returns(false);

        //Act
        _projectBuilder.InitialBuild();

        //Assert
        _processWrapperFactory.Received(0).Create(Arg.Any<ProcessStartInfo>());
    }

    [Test]
    public void GivenSolutionLoadedWithNoProjects_WhenReceivesBuildRequest_ThenNoProcessesStartedAndNoErrors()
    {
        //Arrange
        _statusTracker.TryStartOperation(DarwingOperation.BuildSolution).Returns(true);
        _solutionProvider.IsAvailable.Returns(true);
        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        _solutionProvider.SolutionContainer.Returns(solutionContainer);
        solutionContainer.AllProjects.Returns(new List<IProjectContainer>());

        //Act
        _projectBuilder.InitialBuild();

        //Assert
        _processWrapperFactory.Received(0).Create(Arg.Any<ProcessStartInfo>());
        _statusTracker.Received(1).FinishOperation(DarwingOperation.BuildSolution, true);
    }

    [Test]
    public void GivenSolutionLoadedWithProjects_WhenReceivesBuildRequest_ThenProcessesStartedForEachProject()
    {
        //Arrange
        _statusTracker.TryStartOperation(DarwingOperation.BuildSolution).Returns(true);
        _solutionProvider.IsAvailable.Returns(true);
        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        _solutionProvider.SolutionContainer.Returns(solutionContainer);

        IProjectContainer project1 = Substitute.For<IProjectContainer>();
        project1.Name.Returns("Project1");
        project1.CsprojFilePath.Returns("thi/is/a/path/project1.csproj");
        IProjectContainer project2 = Substitute.For<IProjectContainer>();
        project2.Name.Returns("Project2");
        project2.CsprojFilePath.Returns("thi/is/a/path/project2.csproj");

        solutionContainer.AllProjects.Returns(new List<IProjectContainer>
        {
            project1, project2
        });

        IProcessWrapper process1 = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments.Contains("project1"))).Returns(process1);
        process1.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        process1.Success.Returns(true);

        IProcessWrapper process2 = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments.Contains("project2"))).Returns(process2);
        process2.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        process2.Success.Returns(true);

        //Act
        _projectBuilder.InitialBuild();

        //Assert
        _processWrapperFactory.Received(2).Create(Arg.Any<ProcessStartInfo>()); 
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x => 
            x.FileName == "dotnet" && 
            x.Arguments == "build project1.csproj" &&
            x.WorkingDirectory == "thi\\is\\a\\path" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));

        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "build project2.csproj" &&
            x.WorkingDirectory == "thi\\is\\a\\path" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));
        process1.Received(1).StartAndAwait(Arg.Any<TimeSpan>());
        process2.Received(1).StartAndAwait(Arg.Any<TimeSpan>());
        _statusTracker.Received(1).FinishOperation(DarwingOperation.BuildSolution, true);
    }

    [Test]
    public void GivenSolutionLoadedWithProjects_AndBuildFails_WhenReceivesBuildRequest_ThenCleanAndRestoreProcessStarted()
    {
        //Arrange
        _statusTracker.TryStartOperation(DarwingOperation.BuildSolution).Returns(true);
        _solutionProvider.IsAvailable.Returns(true);
        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        _solutionProvider.SolutionContainer.Returns(solutionContainer);

        IProjectContainer project1 = Substitute.For<IProjectContainer>();
        project1.Name.Returns("Project1");
        project1.CsprojFilePath.Returns("thi/is/a/path/project1.csproj");

        solutionContainer.AllProjects.Returns(new List<IProjectContainer>
        {
            project1
        });

        IProcessWrapper buildProcess = Substitute.For<IProcessWrapper>();
        IProcessWrapper cleanProcess = Substitute.For<IProcessWrapper>();
        IProcessWrapper restoreProcess = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments.Contains("build"))).Returns(buildProcess);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments.Contains("clean"))).Returns(cleanProcess);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments.Contains("restore"))).Returns(restoreProcess);
        buildProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        cleanProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        restoreProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        buildProcess.Success.Returns(false);
        cleanProcess.Success.Returns(true);
        restoreProcess.Success.Returns(true);
        buildProcess.Output.Returns([]);
        cleanProcess.Output.Returns([]);
        restoreProcess.Output.Returns([]);
        buildProcess.Errors.Returns([]);
        cleanProcess.Errors.Returns([]);
        restoreProcess.Errors.Returns([]);

        //Act
        _projectBuilder.InitialBuild();

        //Assert
        _processWrapperFactory.Received(4).Create(Arg.Any<ProcessStartInfo>());
        _processWrapperFactory.Received(2).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "build project1.csproj" &&
            x.WorkingDirectory == "thi\\is\\a\\path" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));

        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "clean project1.csproj" &&
            x.WorkingDirectory == "thi\\is\\a\\path" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));

        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "restore project1.csproj" &&
            x.WorkingDirectory == "thi\\is\\a\\path" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));

        buildProcess.Received(2).StartAndAwait(Arg.Any<TimeSpan>());
        cleanProcess.Received(1).StartAndAwait(Arg.Any<TimeSpan>());
        restoreProcess.Received(1).StartAndAwait(Arg.Any<TimeSpan>());
        _statusTracker.Received(1).FinishOperation(DarwingOperation.BuildSolution, false);
    }
}
