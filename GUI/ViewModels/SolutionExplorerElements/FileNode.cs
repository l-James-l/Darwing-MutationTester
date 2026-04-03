using Models;

namespace GUI.ViewModels.SolutionExplorerElements;

/// <summary>
/// This is a leaf node. represents a source code file
/// </summary>
public sealed class FileNode : SolutionTreeNode
{
    /// <summary>
    /// Is this node the currently selected file.
    /// </summary>
    public override bool IsSelected
    {
        get => field;
        set
        {
            field = value;
            if (value)
            {
                //Notify the owning vm
                _vm.SelectedFile = this;
            }
        }
    }

    public override bool IsChecked
    {
        get;
        set
        {
            SetProperty(ref field, value);
            //If the file is checked, check all the lines in the file. If its unchecked, uncheck all lines.
            if (!_supressFileLinesUpdate &&  value)
            {
                File.LinesToMutate.Fill();
            }
            else if (!_supressFileLinesUpdate)
            {
                File.LinesToMutate.Clear();
            }
            // Invoke the callback so that the updated lines are displayed
            _vm.SelectedFile = this;

            //Notify the parent, unless we were set from the parent in the first place.
            if (!_supressParentNotification)
            {
                Parent?.NotifyCheckedUpdateFromChild();
            }
        }
    }
    private bool _supressParentNotification = false;
    private bool _supressFileLinesUpdate = false;

    /// <summary>
    /// When the IsChecked state of the files parent folder is updated.
    /// Suppresses upwards propagation
    /// </summary>
    public override void NotifyCheckedUpdateFromParent(bool check)
    {
        _supressParentNotification = true;
        IsChecked = check;
        _supressParentNotification = false;
    }

    /// <summary>
    /// When the IsChecked state of any lines in the file are updated.
    /// Suppresses propagation into file lines
    /// </summary>
    /// <param name="check"></param>
    public void NotifyCheckedFromLineInFile(bool? check)
    {
        _supressFileLinesUpdate = true;
        // When check is null, it means the file should self evaluate whether it is checked based on its lines.
        IsChecked = check ?? File.LinesToMutate.Any();
        _supressFileLinesUpdate = false;
    }

    /// <summary>
    /// Gets the list of mutations that were discovered in the file.
    /// </summary>
    public List<DiscoveredMutation> MutationInFile { get; } = [];

    /// <summary>
    /// The file container this node represents
    /// </summary>
    public SourceCodeFileContainer File { get; }
    
    private readonly FileExplorerViewModel _vm;

    public FileNode(SourceCodeFileContainer file, FileExplorerViewModel vm) : base(file.Path)
    {
        File = file;
        _vm = vm;
        IsChecked = file.LinesToMutate.Any();
    }
}
