using System.Globalization;
using GUI.Converters;
using Models.Enums;
using NUnit.Framework;

namespace GUITests.Converters;

[TestFixture]
public class StatusCircleTagConverterTests
{
    private StatusCircleTagConverter _converter; // //SUT

    [SetUp]
    public void SetUp()
    {
        _converter = new StatusCircleTagConverter();
    }

    [Test]
    [TestCase(OperationStates.Succeeded, "✔")]
    [TestCase(OperationStates.Failed, "X")]
    [TestCase(OperationStates.Ongoing, "⌛")]
    public void Convert_GivenKnownStatus_ReturnsExpectedSymbol(OperationStates status, string expectedSymbol)
    {
        // Arrange
        object[] values = { "1", status };

        // Act
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo(expectedSymbol));
    }

    [Test]
    public void Convert_GivenNotStartedStatus_ReturnsOriginalIndexString()
    {
        // Arrange - Assuming there's a NotStarted or similar that isn't Succeeded/Failed/Ongoing
        // If OperationStates is only those 3, use a cast to an undefined enum value to hit the default return
        object[] values = { "5", (OperationStates)999 };

        // Act
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo("5"));
    }

    [Test]
    [TestCaseSource(nameof(GetInvalidInputs))]
    public void Convert_GivenInvalidInputs_ReturnsEmptyString(object[] values)
    {
        // Act
        var result = _converter.Convert(values, typeof(string), null!, CultureInfo.InvariantCulture);

        // Assert
        Assert.That(result, Is.EqualTo(""));
    }

    private static IEnumerable<object[]> GetInvalidInputs()
    {
        yield return new object[] { "1" }; // Wrong length
        yield return new object[] { "1", "NotAnEnum" }; // Wrong type for values[1]
        yield return new object[] { 1, OperationStates.Succeeded }; // values[0] is int, not string
        yield return new object[] { "NotAnInt", OperationStates.Succeeded }; // !int.TryParse
    }

    [Test]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack(null!, null!, null!, CultureInfo.InvariantCulture));
    }
}