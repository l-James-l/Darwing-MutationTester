namespace GUI.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>
/// Base implementation for view models that allows them to notify the view when properties are updated
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Notifies the UI of binding property changes
    /// Should be called from with the setter of the binding property, as this will automatically set the property name.
    /// Otherwise it can still be specified manually
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
