using Core.IndustrialEstate;

namespace CoreTests.IndustrialEstateTests;

public class AnalyzeermanagerFactoryTests
{
    [Test]
    public void GivenNullPath_WhenCreateAnalyzerManager_ThenThrowsArgumentNullException()
    {
        // Arrange
        var factory = new AnalyzerManagerFactory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.CreateAnalyzerManager(null!));
    }

    [Test, Explicit("Must use a real directory, so cant be ensured to pass on any machine.")]
    public void GivenSlnPath_WhenCreateAnalyzerManager_ThenReturnsAnalyzerManagerInstance()
    {
        // Arrange
        var factory = new AnalyzerManagerFactory();
        string slnPath = @"C:\Users\THINKPAD\Documents\git\SimpleTestProject\SimpleTestProject.sln";

        // Act
        var manager = factory.CreateAnalyzerManager(slnPath);

        // Assert
        Assert.IsNotNull(manager);
        Assert.That(manager.GetType().Name, Is.EqualTo("AnalyzerManager"));
    }

    [Test]
    public void GivenEmptyString_WhenCreateAnalyzerManager_ThenReturnsAnalyzerManagerInstance()
    {
        // Arrange
        var factory = new AnalyzerManagerFactory();
        string slnPath = string.Empty;

        // Act
        var manager = factory.CreateAnalyzerManager(slnPath);

        // Assert
        Assert.IsNotNull(manager);
        Assert.That(manager.GetType().Name, Is.EqualTo("AnalyzerManager"));
    }
}
