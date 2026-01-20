using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Data;

[assembly: InternalsVisibleTo("GuiTests")]

namespace GUI.Converters;


internal class CompletionPercentageToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 ||
            values[0] is not double totalWidth||
            values[1] is not float completionPercentage)
        {
            return 0;
        }

        if (completionPercentage < 0)
        {
            return 0;
        }
        if (completionPercentage > 1)
        {
            return totalWidth;
        }
        return totalWidth * completionPercentage;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
