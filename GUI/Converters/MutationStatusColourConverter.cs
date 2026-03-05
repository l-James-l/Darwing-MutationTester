using Models.Enums;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GUI.Converters;

class MutationStatusColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not MutantStatus status)
        {
            return Brushes.Red;
        }

        return status switch
        {
            MutantStatus.Killed => Brushes.Green,
            MutantStatus.KilledByTimeOut => Brushes.Green,
            MutantStatus.Survived => Brushes.Red,
            MutantStatus.NoCoverage => Brushes.Red,
            MutantStatus.Available => Brushes.Orange,
            MutantStatus.TestOngoing => Brushes.Blue,
            MutantStatus.IgnoredMultipleOnLine => Brushes.Gray,
            _ => Brushes.Red,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
