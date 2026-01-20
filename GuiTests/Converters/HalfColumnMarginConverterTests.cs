using GUI.Converters;
using System.Windows;

namespace GuiTests.Converters;

public class HalfColumnMarginConverterTests
{
    private HalfColumnMarginConverter _converter;

    [SetUp]
    public void SetUp()
    {
        _converter = new HalfColumnMarginConverter();
    }

    [Test]
    public void GivenValidInput_ReturnsHalfWidthOfSingleColumn()
    {
        // converter assumes 6 columns
        object value = _converter.Convert(120.0, null, null, null);
        Assert.That(value is Thickness);
        Assert.That(((Thickness)value).Left, Is.EqualTo(10));
        Assert.That(((Thickness)value).Right, Is.EqualTo(10));
        Assert.That(((Thickness)value).Bottom, Is.EqualTo(0));
        Assert.That(((Thickness)value).Top, Is.EqualTo(0));
    }

    [Test]
    public void GivenInvalidInput_ReturnsEmptyThickness()
    {
        object value = _converter.Convert("", null, null, null);
        Assert.That(value is Thickness);
        Assert.That(((Thickness)value).Left, Is.EqualTo(0));
        Assert.That(((Thickness)value).Right, Is.EqualTo(0));
        Assert.That(((Thickness)value).Top, Is.EqualTo(0));
        Assert.That(((Thickness)value).Bottom, Is.EqualTo(0));

    }
}