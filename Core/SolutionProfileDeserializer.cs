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

        _mutationSettings.SolutionProfileData = null;

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
        _mutationSettings.SolutionProfileData = profileData;

        //TODO: as more settings are introduced and used, will need to update them here.
        _mutationSettings.TestProjects = profileData.TestProjects;
        _mutationSettings.IgnoreProjects = profileData.IgnoreProjects;
        _mutationSettings.SourceCodeProjects = profileData.SourceCodeProjects;

        _mutationSettings.SkipTestingNoActiveMutants = profileData.GeneralSettings.SkipTestingNoActiveMutants;
    }
}
