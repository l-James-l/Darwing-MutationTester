using NSubstitute;
using GUI.ViewModels.SolutionExplorerElements;
using Models.Events;
using Models;
using Mutator;
using System.IO.Abstractions.TestingHelpers;
using Models.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GuiTests.ViewModels.SolutionExplorerTests;

[TestFixture]
public class FileExplorerViewModelTests
{
    private ISolutionProvider _solutionProvider;
    private IEventAggregator _eventAggregator;
    private IMutationDiscoveryManager _discoveryManager;
    private MockFileSystem _mockFileSystem;

    private DarwingOperationStatesChangedEvent _operationChangedEvent;
    private Action<DarwingOperation>? _onOperationChangedCallback;
    private MutationUpdated _mutationUpdatedEvent;
    private Action<SyntaxAnnotation>? _mutationUpdatedCallback;
    private GitUpdateEvent _gitUpdateEvent;
    private Action? _gitUpdateCallback;
    private SettingChanged _settingChangedEvent;
    private Action<string>? _settingChangedCallback;

    private FileExplorerViewModel _fileExplorerVm; //SUT

    [SetUp]
    public void SetUp()
    {
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _discoveryManager = Substitute.For<IMutationDiscoveryManager>();

        _mockFileSystem = new MockFileSystem();

        _operationChangedEvent = Substitute.For<DarwingOperationStatesChangedEvent>();
        _mutationUpdatedEvent = Substitute.For<MutationUpdated>();
        _gitUpdateEvent = Substitute.For<GitUpdateEvent>();
        _settingChangedEvent = Substitute.For<SettingChanged>();
        _onOperationChangedCallback = null;
        _settingChangedCallback = null;
        _mutationUpdatedCallback = null;
        _gitUpdateCallback = null;

        _operationChangedEvent.When(x => x.Subscribe(Arg.Any<Action<DarwingOperation>>(), ThreadOption.UIThread, true, Arg.Any<Predicate<DarwingOperation>>()))
            .Do(x => _onOperationChangedCallback = x.Arg<Action<DarwingOperation>>());
        _settingChangedEvent.When(x => x.Subscribe(Arg.Any<Action<string>>(), ThreadOption.UIThread, true, Arg.Any<Predicate<string>>()))
            .Do(x => _settingChangedCallback = x.Arg<Action<string>>());
        _mutationUpdatedEvent.When(x => x.Subscribe(Arg.Any<Action<SyntaxAnnotation>>(), ThreadOption.UIThread))
            .Do(x => _mutationUpdatedCallback = x.Arg<Action<SyntaxAnnotation>>());
        _gitUpdateEvent.When(x => x.Subscribe(Arg.Any<Action>(), ThreadOption.UIThread))
            .Do(x => _gitUpdateCallback = x.Arg<Action>());


        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Returns(_operationChangedEvent);
        _eventAggregator.GetEvent<MutationUpdated>().Returns(_mutationUpdatedEvent);
        _eventAggregator.GetEvent<GitUpdateEvent>().Returns(_gitUpdateEvent);
        _eventAggregator.GetEvent<SettingChanged>().Returns(_settingChangedEvent);

        _fileExplorerVm = new FileExplorerViewModel(_solutionProvider, _eventAggregator, _discoveryManager, _mockFileSystem);
    }

    [Test]
    public void AllSubscriptionsMade()
    {
        Assert.That(_onOperationChangedCallback, Is.Not.Null);
        Assert.That(_mutationUpdatedCallback, Is.Not.Null);
        Assert.That(_settingChangedCallback, Is.Not.Null);
        Assert.That(_gitUpdateCallback, Is.Not.Null);
    }

    [Test]
    public void GivenComplexDirectory_WhenRefreshSolutionTreeCalled_ThenPrunesEmptyFolders()
    {
        // Arrange
        string root = @"C:\MySolution";
        _mockFileSystem.AddDirectory(root);
        // Valid Code Path
        _mockFileSystem.AddFile($@"{root}\ProjectA\Logic.cs", new MockFileData("class A {}"));
        _mockFileSystem.AddFile($@"{root}\ProjectA\ProjectA.csproj", new MockFileData(""));

        // Invalid Path (No .cs files)
        _mockFileSystem.AddDirectory($@"{root}\EmptyFolder");
        _mockFileSystem.AddFile($@"{root}\EmptyFolder\Notes.txt", new MockFileData("just text"));

        // Ignored Path (.git)
        _mockFileSystem.AddDirectory($@"{root}\.git");
        _mockFileSystem.AddFile($@"{root}\.git\config", new MockFileData(""));

        _solutionProvider.IsAvailable.Returns(true);
        _solutionProvider.SolutionContainer.DirectoryPath.Returns(root);

        var mockProject = Substitute.For<IProjectContainer>();
        mockProject.CsprojFilePath.Returns($@"{root}\ProjectA\ProjectA.csproj");
        _solutionProvider.SolutionContainer.SolutionProjects.Returns(new List<IProjectContainer> { mockProject });
        _solutionProvider.SolutionContainer.FindFile(Arg.Any<string>()).Returns(new SourceCodeFileContainer(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText("")));

        // Act
        _onOperationChangedCallback?.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.Multiple(() =>
        {
            // ProjectA has code, it should exist
            Assert.That(_fileExplorerVm.SolutionTree.Any(x => x.Name == "ProjectA"), Is.True);

            // EmptyFolder has no .cs files, it should be pruned
            Assert.That(_fileExplorerVm.SolutionTree.Any(x => x.Name == "EmptyFolder"), Is.False);

            // .git should be explicitly ignored
            Assert.That(_fileExplorerVm.SolutionTree.Any(x => x.Name == ".git"), Is.False);
        });
    }

    [Test]
    public void WhenMutationUpdatedFires_ThenFindsSpecificFileNodeAndUpdateCounts()
    {
        // Arrange
        SourceCodeFileContainer file = new(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText(@"").WithFilePath(@"C:\Src\File.cs"));
        var mutation = new DiscoveredMutation(new SyntaxAnnotation(), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), _eventAggregator, 0, 0)
        {
            Status = MutantStatus.Killed,
            Document = file.DocumentId
        };
        _discoveryManager.DiscoveredMutations.Returns([mutation]);

        _solutionProvider.SolutionContainer.FindFile(Arg.Any<DocumentId>(), ProjectType.Source).Returns(file);

        // Manually add a FileNode into the tree
        FileNode fileNode = new(file, _fileExplorerVm);
        _fileExplorerVm.SolutionTree.Add(fileNode);

        // Act
        // Capture the Action passed to Subscribe and invoke it
        _mutationUpdatedCallback?.Invoke(mutation.ID);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(fileNode.MutationInFile, Contains.Item(mutation));
            Assert.That(fileNode.KilledMutationCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void WhenSelectedFileIsChanged_ThenInvokesCallBack()
    {
        // Arrange
        bool called = false;
        SourceCodeFileContainer file = new(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText(@"").WithFilePath(@"C:\Src\File.cs"));
        var node = new FileNode(file, _fileExplorerVm);
        _fileExplorerVm.SelectedFileChangedCallBack = n => called = true;

        // Act
        _fileExplorerVm.SelectedFile = node;

        // Assert
        Assert.That(called, Is.True);
    }

    [Test]
    public void WhenGitUpdateEventFires_ThenUpdateCheckedStatesIsCalledRecursively()
    {
        // Arrange
        SourceCodeFileContainer file1 = new(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText(@"").WithFilePath(@"C:\Src\File1.cs"));
        SourceCodeFileContainer file2 = new(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText(@"").WithFilePath(@"C:\Src\File2.cs"));

        // We need real instances of nodes because of the recursion and internal logic
        FileNode fileNode1 = new(file1, _fileExplorerVm);
        FileNode fileNode2 = new(file2, _fileExplorerVm);

        fileNode1.IsChecked = true;
        fileNode2.IsChecked = true;

        FolderNode folderNode = new(@"C:\Src\SubFolder");
        folderNode.Children.Add(fileNode2);
        folderNode.IsChecked = true;

        _fileExplorerVm.SolutionTree.Add(fileNode1);
        _fileExplorerVm.SolutionTree.Add(folderNode);
        file1.LinesToMutate.Clear();
        file2.LinesToMutate.Clear();

        // Act
        _gitUpdateCallback?.Invoke();

        // Assert
        Assert.Multiple(() =>
        {
            // Verify top-level file was notified
            Assert.That(fileNode1.IsChecked, Is.False);
            Assert.That(fileNode2.IsChecked, Is.False);
            Assert.That(folderNode.IsChecked, Is.False);
        });
    }

    [Test]
    public void GivenGeneratedFiles_WhenBuildSolutionTreeCalled_ThenExcludesXamlAndGeneratedFiles()
    {
        // Arrange
        string root = @"C:\MySolution";
        _mockFileSystem.AddDirectory(root);
        _mockFileSystem.AddFile($@"{root}\View.xaml.cs", new MockFileData(""));
        _mockFileSystem.AddFile($@"{root}\Generated.g.cs", new MockFileData(""));
        _mockFileSystem.AddFile($@"{root}\Logic.cs", new MockFileData(""));

        _solutionProvider.IsAvailable.Returns(true);
        _solutionProvider.SolutionContainer.DirectoryPath.Returns(root);

        // Mocking FindFile so the builder recognizes the valid .cs file
        _solutionProvider.SolutionContainer.FindFile(Arg.Is<string>(s => s.EndsWith("Logic.cs")))
            .Returns(new SourceCodeFileContainer(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText("").WithFilePath($@"{root}\Logic.cs")));

        // Act
        _onOperationChangedCallback?.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_fileExplorerVm.SolutionTree.Any(x => x.Name.EndsWith("Logic.cs")), Is.True);
            Assert.That(_fileExplorerVm.SolutionTree.Any(x => x.Name.Contains(".g.cs")), Is.False);
            Assert.That(_fileExplorerVm.SolutionTree.Any(x => x.Name.Contains(".xaml.cs")), Is.False);
        });
    }

    [Test]
    public void WhenSourceCodeProjectsSettingChanges_ThenRefreshesSolutionTree()
    {
        // Arrange
        string root = @"C:\MySolution";
        _mockFileSystem.AddFile($@"{root}\NewFile.cs", new MockFileData(""));
        _solutionProvider.IsAvailable.Returns(true);
        _solutionProvider.SolutionContainer.DirectoryPath.Returns(root);
        _solutionProvider.SolutionContainer.FindFile(Arg.Any<string>())
            .Returns(new SourceCodeFileContainer(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText("")));

        // Ensure tree is empty first
        Assert.That(_fileExplorerVm.SolutionTree, Is.Empty);

        // Act
        _settingChangedCallback?.Invoke(nameof(IMutationSettings.SourceCodeProjects));

        // Assert
        Assert.That(_fileExplorerVm.SolutionTree, Has.Count.GreaterThan(0));
    }

    [Test]
    public void WhenSelectedFileSetToNull_ThenDoesNotInvokeCallback()
    {
        // Arrange
        bool called = false;
        _fileExplorerVm.SelectedFileChangedCallBack = n => called = true;

        // Act
        _fileExplorerVm.SelectedFile = null;

        // Assert
        Assert.That(called, Is.False);
    }
}