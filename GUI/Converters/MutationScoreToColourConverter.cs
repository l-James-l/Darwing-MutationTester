
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GUI.Converters;

public class MutationScoreToColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int percentage)
        {
            return Brushes.Red;
        }

        if (percentage < 50)
        {
            return Brushes.Red;
        }
        if (percentage < 85)
        {
            return Brushes.Orange;
        }
        return Brushes.Green;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}