using System.Globalization;
using System.Windows.Data;

namespace GUI.Converters;

/// <summary>
/// When given an integer, adds one to it. Used for displaying 1-based indices in the UI when the underlying data is 0-based.
/// </summary>
public class PlusOneConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int intVal)
        {
            return value;
        }
        return intVal + 1;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}