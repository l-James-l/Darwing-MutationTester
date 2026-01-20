using Models.Enums;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media;

[assembly: InternalsVisibleTo("GuiTests")]

namespace GUI.Converters;

internal class ProssesStateColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not OperationStates state)
        {
            return FromHex("#9CA3AF"); // Gray-400
        }
        return state switch
        {
            OperationStates.NotStarted => FromHex("#9CA3AF"), // Gray-400
            OperationStates.Ongoing => FromHex("#F59E0B"),    // Amber-500
            OperationStates.Succeeded => FromHex("#22C55E"),  // Green-500
            OperationStates.Failed => FromHex("#EF4444"),     // Red-500
            _ => FromHex("#9CA3AF"),                          // Gray-400
        };
    }

    private static SolidColorBrush FromHex(string hex)
    {
        Color colour = (Color)ColorConverter.ConvertFromString(hex)!;
        SolidColorBrush brush = new(colour);
        brush.Freeze();
        return brush;
    }


    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
