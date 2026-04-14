using GUI.Services;
using GUI.ViewModels;
using Models;
using Models.Enums;
using NSubstitute;
using System.IO.Abstractions;
using System.Reflection;

namespace GuiTests.ViewModels.SettingsElements;

[TestFixture]
public class SettingsViewModelTests
{
    private SettingsViewModel _viewModel; // //SUT
    private IMutationSettings _settings;
    private IDarwingDialogService _dialogService;
    private IFileSystem _fileSystem;

    [SetUp]
    public void SetUp()
    {
        _settings = Substitute.For<IMutationSettings>();
        _dialogService = Substitute.For<IDarwingDialogService>();
        _fileSystem = Substitute.For<IFileSystem>();

        _viewModel = new SettingsViewModel(
            default,
            default,
            default,
            _settings,
            _dialogService,
            _fileSystem);
    }

    [Test]
    public void SaveProfile_WhenSolutionPathIsEmpty_ShowsErrorAndReturns()
    {
        // Arrange
        _settings.SolutionPath.Returns("");

        // Act
        _viewModel.SaveProfile();

        // Assert
        _dialogService.Received(1).ErrorDialog("Save Failed", Arg.Any<string>());
        _fileSystem.File.DidNotReceiveWithAnyArgs().WriteAllText(default!, default!);
    }

    [Test]
    public void SaveProfile_WhenSolutionPathIsValid_SerializesAndWritesToFile()
    {
        // Arrange
        const string slnPath = @"C:\Projects\MySln.sln";
        const string expectedDir = @"C:\Projects";
        const string expectedFile = @"C:\Projects\.darwingSolutionProfile.yml";

        _settings.SolutionPath.Returns(slnPath);
        _settings.TestProjects.Returns(new List<string> { "TestProj" });
        _settings.BuildTimeout.Returns(30);

        // Act
        _viewModel.SaveProfile();

        // Assert
        _fileSystem.File.Received(1).WriteAllText(
            Arg.Is<string>(path => path == expectedFile),
            Arg.Is<string>(content => content.Contains("TestProjects") && content.Contains("TestProj"))
        );

        _dialogService.Received(1).InfoDialog("Save Confirmation", Arg.Is<string>(s => s.Contains(expectedFile)));
    }

    [Test]
    public void SaveProfile_WhenDirectoryCannotBeDetermined_ShowsErrorAndReturns()
    {
        // Arrange
        // Path.GetDirectoryName returns null for certain malformed strings or empty strings
        _settings.SolutionPath.Returns("NotAPath");

        // Act
        // On many systems, GetDirectoryName("NotAPath") is empty, not null. 
        // We can force a null by setting a string that doesn't contain directory separators if needed,
        // but often Path.GetDirectoryName("") or very short strings trigger the null check.
        _settings.SolutionPath.Returns("C:");

        // Act
        _viewModel.SaveProfile();

        // Assert
        // This covers the if (directory == null) block
        _dialogService.Received(1).ErrorDialog("Save Failed", Arg.Is<string>(s => s.Contains("directory")));
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

        SolutionProfileData? result = method?.Invoke(_viewModel, null) as SolutionProfileData;

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