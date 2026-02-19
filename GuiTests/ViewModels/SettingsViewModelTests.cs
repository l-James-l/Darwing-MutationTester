using GUI.Services;
using GUI.ViewModels;
using Models;
using Models.Enums;
using NSubstitute;
using System.Reflection;

namespace GuiTests.ViewModels;

public class SettingsViewModelTests
{
    private IMutationSettings _settings;
    private IDarwingDialogService _dialogService;
    private SettingsViewModel _sut;

    [SetUp]
    public void SetUp()
    {
        _settings = Substitute.For<IMutationSettings>();
        _dialogService = Substitute.For<IDarwingDialogService>();

        _sut = new SettingsViewModel(
            default,
            default,
            default,
            _settings,
            _dialogService);
    }

    /// <summary>
    /// This test maps ALL properties from IMutationSettings to the SolutionProfileData returned by BuildNewProfileObject.
    /// 
    /// </summary>
    [Test]
    public void BuildNewProfileObject_MapsAllPropertiesFromSettings()
    {
        // Arrange
        // Have to give non primitives some value to ensure they aren't default/null in the resulting profile
        _settings.TestProjects.Returns([]);
        _settings.IgnoreProjects.Returns(["IgnoreProj"]);
        _settings.SourceCodeProjects.Returns(["sourceProj"]);
        _settings.DisabledMutationTypes.Returns([SpecificMutation.SubtractToAdd]);

        // Act: Invoke the private method via reflection
        MethodInfo? method = 
            typeof(SettingsViewModel).GetMethod("BuildNewProfileObject", BindingFlags.NonPublic | BindingFlags.Instance);

        SolutionProfileData? result = method?.Invoke(_sut, null) as SolutionProfileData;

        // Assert
        PropertyInfo[] profileProperties = typeof(SolutionProfileData).GetProperties();

        // Assert that none of the properties in the resulting profile are default values, which would indicate they were not mapped from the settings
        Assert.Multiple(() =>
        {
            foreach (var prop in profileProperties)
            {
                object? val = prop.GetValue(result);

                Assert.That(val, Is.Not.Default, $"Property {prop.Name} was not assigned!");
            }
        });
    }
}