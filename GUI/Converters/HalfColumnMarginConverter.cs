using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

[assembly: InternalsVisibleTo("GuiTests")]

namespace GUI.Converters;

internal class HalfColumnMarginConverter : IValueConverter
{
    /// <summary>
    /// Will take the width of a column, and halve it.
    /// Used to ensure that the status bar does not exceed the limits of the first and last stages
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Safe cast. Should always be true
        if (value is not double totalWidth || totalWidth <= 0)
        {
            return new Thickness(0);
        }

        const int columnCount = 6;

        double columnWidth = totalWidth / columnCount;
        double halfColumn = columnWidth / 2;

        return new Thickness(halfColumn, 0, halfColumn, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
