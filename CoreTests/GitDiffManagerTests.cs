using Core;
using Core.IndustrialEstate;
using LibGit2Sharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;
using Models.Enums;
using Models.Events;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Serilog;
using Serilog.Sinks.TestCorrelator;
using System.Collections;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace CoreTests;

public class GitDiffManagerTests
{
    private ISolutionProvider _solutionProvider;
    private IMutationSettings _settings;
    private IEventAggregator _eventAggregator;
    private IRepositoryFactory _repositoryFactory;
    private IRepository _mockRepo;
    private GitUpdateEvent _mockGitEvent;
    private IFileSystem _fileSystem;

    private GitDiffManager _sut; //SUT

    [SetUp]
    public void SetUp()
    {
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _settings = Substitute.For<IMutationSettings>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _repositoryFactory = Substitute.For<IRepositoryFactory>();
        _mockRepo = Substitute.For<IRepository>();
        _mockGitEvent = Substitute.For<GitUpdateEvent>();

        // Using MockFileSystem for easier setup, but you can use Substitute.For<IFileSystem>() if preferred
        _fileSystem = new MockFileSystem();

        _eventAggregator.GetEvent<GitUpdateEvent>().Returns(_mockGitEvent);
        _repositoryFactory.Get(Arg.Any<string>()).Returns(_mockRepo);

        // Setup a dummy solution path
        var container = Substitute.For<ISolutionContainer>();
        string tempDir = @"C:\MockSolution";
        container.DirectoryPath.Returns(tempDir);

        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(tempDir, ".git"));

        _solutionProvider.SolutionContainer.Returns(container);
        _solutionProvider.SolutionContainer.DirectoryPath.Returns(tempDir);

        _sut = new GitDiffManager(_solutionProvider, _settings, _eventAggregator, _repositoryFactory, _fileSystem); 
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
        _mockRepo.Dispose();
    }

    [Test]
    public void GivenNoGitDirectoryExists_WhenInitialGitDiffIsCalled_ThenPublishesUpdateEventAndReturns()
    {
        // Arrange
        var nonExistentPath = @"C:\NonExistent";
        _solutionProvider.SolutionContainer.DirectoryPath.Returns(nonExistentPath);

        // Act
        _sut.InitialGitDiff();

        // Assert
        _mockGitEvent.Received(1).Publish();
        Assert.That(_sut.LastSelectedBranch, Is.Null);
    }

    [Test]
    public void GivenLoadingGitRepoThrowsException_WhenInitialGitDiffIsCalled_ThenExceptionCaught_AndNoDiffSet()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(_solutionProvider.SolutionContainer.DirectoryPath, ".git"));
        _settings.DefaultGitComparisonBranch.Returns("main");
        _repositoryFactory.Get(Arg.Any<string>()).Throws<Exception>();

        // Act
        _sut.InitialGitDiff();

        // Assert
        _repositoryFactory.Received().Get(Arg.Is<string>(s => s.EndsWith(".git")));
        _mockGitEvent.Received().Publish();
        Assert.That(_sut.LastSelectedBranch, Is.Null);
    }

    [Test]
    public void GivenLoadingGitRepoReturnsNull_WhenInitialGitDiffIsCalled_ThenNoExceptionThrown_AndNoDiffSet()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(_solutionProvider.SolutionContainer.DirectoryPath, ".git"));
        _settings.DefaultGitComparisonBranch.Returns("main");
        _repositoryFactory.Get(Arg.Any<string>()).ReturnsNull();

        // Act
        _sut.InitialGitDiff();

        // Assert
        _repositoryFactory.Received().Get(Arg.Is<string>(s => s.EndsWith(".git")));
        _mockGitEvent.Received().Publish();
        Assert.That(_sut.LastSelectedBranch, Is.Null);
    }

    [Test]
    public void GivenLoadValidRepo_ButNoBranchFound_WhenInitialGitDiffIsCalled_ThenNoExceptionThrown_AndNoDiffSet()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(_solutionProvider.SolutionContainer.DirectoryPath, ".git"));
        _settings.DefaultGitComparisonBranch.Returns("main");
        BranchCollection branchCollection = Substitute.For<BranchCollection>();
        _mockRepo.Branches.Returns(branchCollection);

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        _sut.InitialGitDiff();

        // Assert
        _repositoryFactory.Received().Get(Arg.Is<string>(s => s.EndsWith(".git")));
        _mockGitEvent.Received().Publish();
        Assert.That(_sut.LastSelectedBranch, Is.Null);
        var log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text == "Couldn't find branch {branch}");
        Assert.That(log, Is.Not.Null);
    }

    [Test]
    public void GivenLoadValidRepoAndBranch_ButDiffThrowsException_WhenInitialGitDiffIsCalled_ThenNoExceptionThrown_AndNoDiffSet()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(_solutionProvider.SolutionContainer.DirectoryPath, ".git"));
        _settings.DefaultGitComparisonBranch.Returns("main");

        BranchCollection branchCollection = Substitute.For<BranchCollection>();
        var mockBranch = Substitute.For<Branch>();
        mockBranch.FriendlyName.Returns("main");
        var branchList = new List<Branch> { mockBranch };
        branchCollection.GetEnumerator().Returns(_ => branchList.GetEnumerator());
        ((IEnumerable)branchCollection).GetEnumerator().Returns(_ => branchList.GetEnumerator());
        _mockRepo.Branches.Returns(branchCollection);

        _mockRepo.Diff.Compare(default, default).Throws<Exception>();

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        _sut.InitialGitDiff();

        // Assert
        _repositoryFactory.Received().Get(Arg.Is<string>(s => s.EndsWith(".git")));
        _mockGitEvent.Received().Publish();
        Assert.That(_sut.LastSelectedBranch, Is.Null);
        var log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text == "Failed to get git diff.");
        Assert.That(log, Is.Not.Null);
    }

    [Test]
    public void GivenLoadValidRepoAndBranch_ButDiffReturnsNull_WhenInitialGitDiffIsCalled_ThenNoExceptionThrown_AndNoDiffSet()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(_solutionProvider.SolutionContainer.DirectoryPath, ".git"));
        _settings.DefaultGitComparisonBranch.Returns("main");

        BranchCollection branchCollection = Substitute.For<BranchCollection>();
        var mockBranch = Substitute.For<Branch>();
        mockBranch.FriendlyName.Returns("main");
        var branchList = new List<Branch> { mockBranch };
        branchCollection.GetEnumerator().Returns(_ => branchList.GetEnumerator());
        ((IEnumerable)branchCollection).GetEnumerator().Returns(_ => branchList.GetEnumerator());
        _mockRepo.Branches.Returns(branchCollection);

        _mockRepo.Diff.Compare(default, default).ReturnsNull();

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        _sut.InitialGitDiff();

        // Assert
        _repositoryFactory.Received().Get(Arg.Is<string>(s => s.EndsWith(".git")));
        _mockGitEvent.Received().Publish();
        Assert.That(_sut.LastSelectedBranch, Is.Null);
        var log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text == "Couldn't establish a git diff to {branch}.");
        Assert.That(log, Is.Not.Null);
    }

    [Test]
    public void GivenPatchWithAddedLines_WhenEstablishDiffIsCalled_ThenLinesToMutateArePopulated()
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory(_fileSystem.Path.Combine(_solutionProvider.SolutionContainer.DirectoryPath, ".git"));
        _settings.DefaultGitComparisonBranch.Returns("main");

        // Will load the repo
        _sut.InitialGitDiff();

        BranchCollection branchCollection = Substitute.For<BranchCollection>();
        var mockBranch = Substitute.For<Branch>();
        mockBranch.FriendlyName.Returns("main");
        Commit commit = Substitute.For<Commit>();
        commit.Tree.Returns(Substitute.For<Tree>());
        mockBranch.Tip.Returns(commit);
        var branchList = new List<Branch> { mockBranch };
        branchCollection.GetEnumerator().Returns(_ => branchList.GetEnumerator());
        ((IEnumerable)branchCollection).GetEnumerator().Returns(_ => branchList.GetEnumerator());
        _mockRepo.Branches.Returns(branchCollection);

        string fileContent =
@" public void GivenLoadValidRepoAndBranch_ButDiffReturnsNull_WhenInitialGitDiffIsCalled_ThenNoExceptionThrown_AndNoDiffSet()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_solutionProvider.SolutionContainer.DirectoryPath, "".git""));
        _settings.DefaultGitComparisonBranch.Returns(""main"");

        BranchCollection branchCollection = Substitute.For<BranchCollection>();
        var mockBranch = Substitute.For<Branch>();
        mockBranch.FriendlyName.Returns(""main"");
        var branchList = new List<Branch> { mockBranch };
        branchCollection.GetEnumerator().Returns(_ => branchList.GetEnumerator());
        ((IEnumerable)branchCollection).GetEnumerator().Returns(_ => branchList.GetEnumerator()); _mockRepo.Branches.Returns(branchCollection);

        _mockRepo.Diff.Compare(default, default).ReturnsNull();

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        _sut.InitialGitDiff();

        // Assert
        _repositoryFactory.Received().Get(Arg.Is<string>(s => s.EndsWith("".git"")));
        _mockGitEvent.Received().Publish();
        Assert.That(_sut.LastSelectedBranch, Is.Null);
        var log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text == ""Couldn't establish a git diff to {branch}."");
        Assert.That(log, Is.Not.Null);
    }";

        var project = Substitute.For<IProjectContainer>();
        SourceCodeFileCollection projectFileCollection = new();
        projectFileCollection.AddDocument(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText(fileContent).WithFilePath(@"C:\Code\File.cs"));
        project.FileCollection.Returns(projectFileCollection);
        _solutionProvider.SolutionContainer.SolutionProjects.Returns([project]);
        var sourceFile = projectFileCollection.First();
        _solutionProvider.SolutionContainer.FindFile(Arg.Any<string>(), Arg.Any<ProjectType>()).Returns(sourceFile);

        var patch = Substitute.For<Patch>();
        var change = Substitute.For<PatchEntryChanges>();
        change.Path.Returns("File.cs");

        // Create added line. doesn't have public setters and cant be mocked so use refection
        var line = (Line)Activator.CreateInstance(
            typeof(Line),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [10, "public void NewMethod()"],
            null)!;
        change.AddedLines.Returns(new List<Line> { line });

        patch.GetEnumerator().Returns(new List<PatchEntryChanges> { change }.GetEnumerator());
        _mockRepo.Diff.Compare<Patch>(Arg.Any<Tree>(), DiffTargets.WorkingDirectory).Returns(patch);

        // Act
        _sut.EstablishDiff("main");

        // Assert
        Assert.Multiple(() =>
        {
            for (int x= 0; x<=28; x++)
            {
                if (x == 9)
                {
                    continue;
                }
                Assert.That(sourceFile.LinesToMutate.ContainsLine(x), Is.False); 
            }
            Assert.That(sourceFile.LinesToMutate.ContainsLine(9), Is.True); 
            Assert.That(_sut.LastSelectedBranch, Is.EqualTo("main"));
            _mockGitEvent.Received().Publish();
        });
    }
}