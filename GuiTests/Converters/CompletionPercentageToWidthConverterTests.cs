using GUI.Converters;

namespace GuiTests.Converters;

public class CompletionPercentageToWidthConverterTests
{
    private CompletionPercentageToWidthConverter _converter;

    [SetUp]
    public void Setup()
    {
        _converter = new CompletionPercentageToWidthConverter();
    }

    [Test]
    public void Convert_ValidInputs_ReturnsExpectedWidth()
    {
        object[] values = { 200.0, 0.5f };
        var result = _converter.Convert(values, null, null, null);
        Assert.That(result, Is.EqualTo(100.0));
    }

    [Test]
    public void Convert_NegativePercentage_ReturnsZero()
    {
        object[] values = { 200.0, -0.1f };
        var result = _converter.Convert(values, null, null, null);
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void Convert_PercentageGreaterThanOne_ReturnsTotalWidth()
    {
        object[] values = { 200.0, 1.5f };
        var result = _converter.Convert(values, null, null, null);
        Assert.That(result, Is.EqualTo(200.0));
    }

    [Test]
    public void Convert_InvalidInputs_ReturnsZero()
    {
        object[] values = { "invalid", 0.5f };
        var result = _converter.Convert(values, null, null, null);
        Assert.That(result, Is.EqualTo(0));
    }
}
