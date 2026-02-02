using Models.Enums;
using System.Globalization;
using System.Windows.Data;

namespace GUI.Converters;

class MutationStateStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not MutantStatus status)
        {
            return "Awaiting Test";
        }

        return status switch
        {
            MutantStatus.Survived => "Killed",
            MutantStatus.Killed => "Survived",
            _ => "Awaiting Test"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
