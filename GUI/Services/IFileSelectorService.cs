namespace GUI.Services;


/// <summary>
/// DI-able interface to a file picker
/// </summary>
public interface IFileSelectorService
{
    /// <summary>
    /// Service which will open a standard windows file picker and return the selected file path.
    /// </summary>
    /// <param name="filter">Which files can be selected</param>
    /// <returns>The selected file type</returns>
    string? OpenFileDialog(string filter = "");
}
