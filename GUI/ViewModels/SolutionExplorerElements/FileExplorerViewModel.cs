using GUI.Services;
using Microsoft.CodeAnalysis;
using Models;
using Models.Enums;
using Models.Events;
using Mutator;
using Serilog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace GUI.ViewModels.SolutionExplorerElements;

/// <summary>
/// View model for the solution explorer tree, showing all the projects and their contained files.
/// </summary>
public class FileExplorerViewModel : ViewModelBase
{
    private readonly ISolutionProvider _solutionProvider;
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationDiscoveryManager _mutationDiscoveryManager;

    public FileExplorerViewModel(ISolutionProvider solutionProvider, IEventAggregator eventAggregator,
        IMutationDiscoveryManager mutationDiscoveryManager)
    {
        _solutionProvider = solutionProvider;
        _eventAggregator = eventAggregator;
        _mutationDiscoveryManager = mutationDiscoveryManager;

        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Subscribe(_ => OnSolutionLoaded(),
            ThreadOption.UIThread, true, x => x == DarwingOperation.LoadSolution);
        _eventAggregator.GetEvent<MutationUpdated>().Subscribe(OnMutationUpdated, ThreadOption.UIThread);
    }

    /// <summary>
    /// The file node for the currently selected file.
    /// Null if no file has been selected yet
    /// </summary>
    public FileNode? SelectFile
    {
        get;
        set
        {
            FileNode? prevValue = field;
            SetProperty(ref field, value);
            if (prevValue != value && value is not null)
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
        if (_solutionProvider.SolutionContainer.Solution.GetDocument(mutation.Document) is not { FilePath: not null } doc)
        {
            Log.Warning("Couldn't get the document for mutation {mutationID}", mutation.ID);
            return;
        }

        FileNode? matchingFileNode = FindFileNode(doc.FilePath, SolutionTree);
        
        if (matchingFileNode is null)
        {
            Log.Warning("No file node found for a reported mutation.");
            return;
        }

        if (!matchingFileNode.MutationInFile.Contains(mutation))
        {
            matchingFileNode.MutationInFile.Add(mutation);
        }
        matchingFileNode.MutationCount = matchingFileNode.MutationInFile.Count(x => x.Status is not MutantStatus.CausedBuildError);
        matchingFileNode.KilledMutationCount = matchingFileNode.MutationInFile.Count(x => x.Status is MutantStatus.Killed);
        OnPropertyChanged(nameof(SolutionTree));
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

    private void OnSolutionLoaded()
    {
        SolutionTree.Clear();
        if (!_solutionProvider.IsAvailable || _solutionProvider.SolutionContainer.Solution.FilePath is null)
        {
            return;
        }

        string? solutionFolder = Path.GetDirectoryName(_solutionProvider.SolutionContainer.Solution.FilePath);
        FolderNode dummyRoot = new (solutionFolder ?? "");
        BuildSolutionTree(dummyRoot);
        SolutionTree.AddRange(dummyRoot.Children);
    }

    private void BuildSolutionTree(FolderNode parentFolder)
    {
        foreach (string filePath in Directory.GetFiles(parentFolder.FullPath, "*.cs", SearchOption.TopDirectoryOnly))
        {
            string fileName = Path.GetFileName(filePath);
            if (fileName.EndsWith(".xaml.cs") || fileName.EndsWith(".g.cs") || fileName.EndsWith(".g.i.cs"))
            {
                continue;
            }
            FileNode fileNode = new(filePath, this)
            {
                Parent = parentFolder
            };
            parentFolder.Children.Add(fileNode);

            //Because we've added this code file, we now know the parent has at least 1 code file
            parentFolder.ContainsCodeFiles = true;
        }

        foreach (string dir in Directory.GetDirectories(parentFolder.FullPath, "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileName(dir);
            if (name.EndsWith(".git") || name.EndsWith("bin") || name.EndsWith("obj") || name.StartsWith('.'))
            {
                continue;
            }

            string wouldBeProjFile = Path.Combine(dir, $"{name}.csproj");
            if (File.Exists(wouldBeProjFile))
            {
                if (_solutionProvider.SolutionContainer.TestProjects.Any(x => x.CsprojFilePath == wouldBeProjFile))
                {
                    // Dont want to show test projects in the main tree.
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
