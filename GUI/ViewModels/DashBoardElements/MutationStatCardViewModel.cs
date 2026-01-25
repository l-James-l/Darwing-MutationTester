using System.Windows.Media;

namespace GUI.ViewModels.DashBoardElements;

public class MutationStatCardViewModel : ViewModelBase
{
    /// <summary>
    /// Title displayed on the card
    /// </summary>
    public string Title
    {
        get { return _title; }
        set
        {
            _title = value;
            OnPropertyChanged();
        }
    }
    private string _title = "";

    /// <summary>
    /// Binding property for how the value is displayed
    /// </summary>
    public string ValueString
    {
        get => _valueString;
        set
        {
            _valueString = value;
            OnPropertyChanged();
        }
    }
    private string _valueString = "";

    /// <summary>
    /// The actual value
    /// </summary>
    public int Value
    {
        get => _value;
        set
        {
            _value = value;
            ValueString = $"{_value}{ValueSuffix}";
        }
    }
    private int _value = 0;

    /// <summary>
    /// Suffix to be applied to the value to result in the <see cref="ValueString"/>
    /// </summary>
    public string ValueSuffix
    {
        get => _valueSuffix;
        set
        {
            _valueSuffix = value;
            ValueString = $"{_value}{ValueSuffix}";
        }
    }
    private string _valueSuffix = "";

    /// <summary>
    /// Substile displayed on the card
    /// </summary>
    public string Subtitle
    {
        get => _subtitle;
        set
        {
            _subtitle = value;
            OnPropertyChanged();
        }
    }
    private string _subtitle = "";

    /// <summary>
    /// Icon displayed on the card
    /// </summary>
    public string Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            OnPropertyChanged();
        }
    }
    private string _icon = "";

    /// <summary>
    /// Colour of the icon
    /// </summary>
    public Brush IconBrush
    {
        get => _iconBrush;
        set
        {
            _iconBrush = value;
            OnPropertyChanged();
        }
    }
    private Brush _iconBrush = Brushes.Blue;
}