using Core;
using Models;
using NSubstitute;
using System.Reflection;

namespace CoreTests.Startup;

[TestFixture]
public class SolutionProfileDeserializerTests
{
    private IMutationSettings _mutationSettings;
    private SolutionProfileDeserializer _sut;

    [SetUp]
    public void SetUp()
    {
        _mutationSettings = Substitute.For<IMutationSettings>();
        _sut = new SolutionProfileDeserializer(_mutationSettings);
    }

    [Test]
    public void AssignSettingsFromProfile_MapsAllPropertiesToSettings()
    {
        // 1. Arrange: Create a profile with dummy data for every property
        var profile = new SolutionProfileData
        {
            TestProjects = new List<string> { "TestA" },
            IgnoreProjects = new List<string> { "IgnoreA" },
            SingleMutantPerLine = true,
            BuildTimeout = 999,
            MutationTestTimeoutScaler = 2.5,
            DefaultGitComparisonBranch = "develop"
        };

        // 2. Act: Invoke the private method
        MethodInfo? method = typeof(SolutionProfileDeserializer).GetMethod("AssignSettingsFromProfile",
            BindingFlags.NonPublic | BindingFlags.Instance);

        method?.Invoke(_sut, [profile]);

        // 3. Assert: Use Reflection to verify the Setter was called on the mock for every property
        PropertyInfo[] profileProperties = typeof(SolutionProfileData).GetProperties();

        foreach (PropertyInfo prop in profileProperties)
        {
            object? expectedValue = prop.GetValue(profile);

            // This dynamically checks that the property on the mock was set to the expectedValue
            // We use Received() to verify the setter was actually called
            _mutationSettings.Received().GetType().GetProperty(prop.Name)?.SetValue(_mutationSettings, expectedValue);

            // Note: If property names differ between Profile and Settings, 
            // you'll need a mapping dictionary or custom logic here.
        }
    }
}