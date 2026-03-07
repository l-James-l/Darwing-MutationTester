using GUI.Services;
using Microsoft.CodeAnalysis;
using Models;
using Models.Enums;
using Models.Events;
using Mutator;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;

namespace GUI.ViewModels.SolutionExplorerElements;

/// <summary>
/// View model for the solution explorer tree, showing all the projects and their contained files.
/// </summary>
public class FileExplorerViewModel : ViewModelBase
{
    private readonly ISolutionProvider _solutionProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationDiscoveryManager _mutationDiscoveryManager;
    private readonly IFileSystem _fileSystem;

    public FileExplorerViewModel(ISolutionProvider solutionProvider, IEventAggregator eventAggregator,
        IMutationDiscoveryManager mutationDiscoveryManager, IFileSystem fileSystem)
    {
        _solutionProvider = solutionProvider;
        _eventAggregator = eventAggregator;
        _mutationDiscoveryManager = mutationDiscoveryManager;
        _fileSystem = fileSystem;

        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Subscribe(_ => RefreshSolutionTree(),
            ThreadOption.UIThread, true, x => x == DarwingOperation.LoadSolution);
        _eventAggregator.GetEvent<MutationUpdated>().Subscribe(OnMutationUpdated, ThreadOption.UIThread);
        _eventAggregator.GetEvent<SettingChanged>().Subscribe(_ => RefreshSolutionTree(), ThreadOption.UIThread, true, x => x == nameof(IMutationSettings.SourceCodeProjects));
        _eventAggregator.GetEvent<GitUpdateEvent>().Subscribe(() => UpdateCheckedStates(SolutionTree), ThreadOption.UIThread);
    }

    public void UpdateCheckedStates(IEnumerable<SolutionTreeNode> solutionTree)
    {
        foreach (FileNode file in solutionTree.Where(x => x is FileNode))
        {
            file.NotifyCheckedFromLineInFile(null); //Pass null to get the file to self evaluate.
        }
        foreach (FolderNode folder in solutionTree.Where(x => x is FolderNode))
        {
            UpdateCheckedStates(folder.Children);
            folder.NotifyCheckedUpdateFromChild();
        }
    }

    /// <summary>
    /// The file node for the currently selected file.
    /// Null if no file has been selected yet
    /// </summary>
    public FileNode? SelectedFile
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (value is not null)
            {
                SelectedFileChangedCallBack?.Invoke(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the callback action to invoke when the selected file changes.
    /// </summary>
    /// <remarks>Assign a method to this property to handle file selection changes. The callback receives the
    /// path of the newly selected file as a parameter. If not set, no action is taken when the selected file
    /// changes.</remarks>
    public Action<FileNode>? SelectedFileChangedCallBack { get; set; }

    /// <summary>
    /// The tree that contains all the different folders, projects, and .cs files
    /// Excludes test projects
    /// 
    /// TODO make a separate tree to allow showing the test projects separately.
    /// </summary>
    public ObservableCollection<SolutionTreeNode> SolutionTree { get; private set; } = [];

    private void OnMutationUpdated(SyntaxAnnotation mutationID)
    {
        if (_mutationDiscoveryManager.DiscoveredMutations.FirstOrDefault(x => x.ID == mutationID) is not { } mutation)
        {
            Log.Warning("Received update for unknown mutation {MutationID}", mutationID);
            return;
        }
        if (_solutionProvider.SolutionContainer.FindFile(mutation.Document, ProjectType.Source) is not { } doc)
        {
            Log.Warning("Couldn't get the document for mutation {mutationID}", mutation.ID);
            return;
        }

        FileNode? matchingFileNode = FindFileNode(doc.Path, SolutionTree);
        
        if (matchingFileNode is null)
        {
            Log.Warning("No file node found for a reported mutation.");
            return;
        }

        if (mutation.Status.IncludeInReport() && !matchingFileNode.MutationInFile.Contains(mutation))
        {
            matchingFileNode.MutationInFile.Add(mutation);
        }
        matchingFileNode.MutationCount = matchingFileNode.MutationInFile.Count(x => x.Status.IncludeInTotalCount());
        matchingFileNode.KilledMutationCount = matchingFileNode.MutationInFile.Count(x => x.Status.IncludeInKilledCount());
        OnPropertyChanged(nameof(SolutionTree));

        if (SelectedFile is not null &&  matchingFileNode == SelectedFile)
        {
            SelectedFileChangedCallBack?.Invoke(matchingFileNode);
        }
    }

    private FileNode? FindFileNode(string filePath, IEnumerable<SolutionTreeNode> nodes)
    {
        foreach (SolutionTreeNode node in nodes)
        {
            if (node is FileNode file && filePath == node.FullPath)
            {
                return file;
            }
            if (node is FolderNode folder && filePath.StartsWith(folder.FullPath))
            {
                return FindFileNode(filePath, folder.Children);
            }
        }

        return null;
    }

    private void RefreshSolutionTree()
    {
        SolutionTree.Clear();
        if (!_solutionProvider.IsAvailable)
        {
            return;
        }

        FolderNode dummyRoot = new (_solutionProvider.SolutionContainer.DirectoryPath);
        BuildSolutionTree(dummyRoot);
        SolutionTree.AddRange(dummyRoot.Children);
    }

    private void BuildSolutionTree(FolderNode parentFolder)
    {
        foreach (string filePath in _fileSystem.Directory.GetFiles(parentFolder.FullPath, "*.cs", SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".xaml.cs") || fileName.EndsWith(".g.cs") || fileName.EndsWith(".g.i.cs"))
            {
                continue;
            }
            if (_solutionProvider.SolutionContainer.FindFile(filePath) is not { } file)
            {
                // File not found, ignore it
                Log.Warning($"'{filePath}' encountered in solution tree construction but not in solution.");
                continue;
            }
            FileNode fileNode = new(file, this)
            {
                Parent = parentFolder
            };
            parentFolder.Children.Add(fileNode);

            //Because we've added this code file, we now know the parent has at least 1 code file
            parentFolder.ContainsCodeFiles = true;
        }

        foreach (string dir in _fileSystem.Directory.GetDirectories(parentFolder.FullPath, "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(dir);
            if (name.EndsWith(".git") || name.EndsWith("bin") || name.EndsWith("obj") || name.StartsWith('.'))
            {
                continue;
            }

            string wouldBeProjFile = Path.Combine(dir, $"{name}.csproj");
            if (_fileSystem.File.Exists(wouldBeProjFile))
            {
                if (!_solutionProvider.SolutionContainer.SolutionProjects.Any(x => x.CsprojFilePath == wouldBeProjFile))
                {
                    // Dont want to show test or ignored projects in the main tree.
                    // Can show these separately later.
                    continue;
                }
                ProjectNode projectNode = new(dir)
                {
                    Parent = parentFolder
                };
                parentFolder.Children.Add(projectNode);
                BuildSolutionTree(projectNode);
            }
            else
            {
                FolderNode folderNode = new(dir)
                {
                    Parent = parentFolder
                };
                parentFolder.Children.Add(folderNode);
                BuildSolutionTree(folderNode);
            }
        }

        // Check for nested folders containing code files
        if (parentFolder.Children.Any(x => x is FolderNode { ContainsCodeFiles: true }))
        {
            parentFolder.ContainsCodeFiles = true;
        }

        // Remove any pointless folder from the tree. 
        parentFolder.Children.RemoveWhere(x => x is FolderNode { ContainsCodeFiles: false });
    }
}
