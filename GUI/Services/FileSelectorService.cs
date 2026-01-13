using Microsoft.Win32;
using Serilog;

namespace GUI.Services;

/// <inheritdoc/>
public class FileSelectorService : IFileSelectorService
{
    /// <inheritdoc/>
    public string? OpenFileDialog(string filter = "All Files|*.*")
    {
        try
        {
            OpenFileDialog dialog = new()
            {
                Filter = filter
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                return dialog.FileName;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error encountered while trying to load or interact with file selector.", ex);
        }

        return null;
    }
}
