using System.Collections.ObjectModel;
using System.IO;

namespace GUI.ViewModels.SolutionExplorerElements;


/// <summary>
/// Base class for all the nodes used in the solution explorer tree view.
/// All classes in the same file because they are so closely related, separating them would only be detrimental.
/// </summary>
public abstract class SolutionTreeNode
{
    /// <summary>
    /// Name of the file or folder represented by this node
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The absolute path to the file or directory represented by this instance.
    /// </summary>
    public string FullPath { get; }

    protected SolutionTreeNode(string fullPath)
    {
        ArgumentNullException.ThrowIfNull(fullPath);
        FullPath = fullPath;
        Name = Path.GetFileName(fullPath);
    }
}

/// <summary>
/// This is a leaf node. represents a source code file
/// </summary>
public sealed class FileNode : SolutionTreeNode
{
    /// <summary>
    /// Is this node the currently selected file.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            if (value)
            {
                //Notify the owning vm
                _vm.SelectFilePath = FullPath;
            }
        }
    }
    private bool _isSelected = false;

    private readonly FileExplorerViewModel _vm;

    public FileNode(string fullPath, FileExplorerViewModel vm) : base(fullPath)
    {
        _vm = vm;
    }
}

/// <summary>
/// Represents a folder in the solution explorer tree
/// </summary>
public class FolderNode : SolutionTreeNode
{
    public ObservableCollection<SolutionTreeNode> Children { get; }

    /// <summary>
    /// By tracking if the folder contains any code files, we can ignore folders that do not contain any code files.
    /// Such as folders that only contain resources, documentation, or binaries.
    /// </summary>
    public bool ContainsCodeFiles { get; set; } = false;

    public FolderNode(string fullPath) : base(fullPath)
    {
        Children = [];
    }
}

/// <summary>
/// Represents a project folder node within a hierarchical structure, such as a solution or workspace.
/// Adds no new functionality beyond FolderNode, but by being a distinct type, it allows for specific handling in the tree view.
/// </summary>
public sealed class ProjectNode : FolderNode
{
    public ProjectNode(string fullPath) : base(fullPath)
    {

    }
}