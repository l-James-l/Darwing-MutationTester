using Core.Interfaces;
using Models;
using Serilog;
using YamlDotNet.Serialization;

namespace Core;

public class SolutionProfileDeserializer : ISolutionProfileDeserializer
{
    private const string solutionProfileFileName = ".darwingSolutionProfile.yml";

    private readonly IMutationSettings _mutationSettings;

    public SolutionProfileDeserializer(IMutationSettings mutationSettings)
    {
        ArgumentNullException.ThrowIfNull(mutationSettings);

        _mutationSettings = mutationSettings;
    }

    public void LoadSlnProfileIfPresent(string slnFilePath)
    {
        ArgumentNullException.ThrowIfNull(slnFilePath);

        _mutationSettings.UpdateProfile(null);

        string? directory = Path.GetDirectoryName(slnFilePath);
        if (directory == null)
        {
            Log.Error("Failed to determine solution directory from solution file path.");
            return;
        }
        if (!File.Exists(Path.Combine(directory, solutionProfileFileName)))
        {
            Log.Information("No solution profile file found in solution directory.");
            return;
        }

        var deserializer = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

        try
        {
            string ymlContent = File.ReadAllText(Path.Combine(directory, solutionProfileFileName));

            SolutionProfileData profileData = deserializer.Deserialize<SolutionProfileData>(ymlContent);
            Log.Information("Successfully loaded solution profile data.");

            AssignSettingsFromProfile(profileData);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to deserialize the provided solution profile data. Continuing without it.");
            Log.Debug($"Exception: {ex}");
        }
    }

    private void AssignSettingsFromProfile(SolutionProfileData profileData)
    {
        //  Assign the loaded profile data to the mutation settings, so that it can be used by other components in the system.
        _mutationSettings.UpdateProfile(profileData);

        _mutationSettings.TestProjects = profileData.TestProjects;
        _mutationSettings.IgnoreProjects = profileData.IgnoreProjects;
        _mutationSettings.SourceCodeProjects = profileData.SourceCodeProjects;
        _mutationSettings.DisabledMutationTypes = profileData.DisabledMutationTypes;

        _mutationSettings.SingleMutantPerLine = profileData.GeneralSettings.SingleMutantPerLine;
        _mutationSettings.BuildTimeout = profileData.GeneralSettings.BuildTimeout;
        _mutationSettings.TestRunTimeout = profileData.GeneralSettings.TestRunTimeout;
        _mutationSettings.SkipTestingNoActiveMutants = profileData.GeneralSettings.SkipTestingNoActiveMutants;
    }
}
