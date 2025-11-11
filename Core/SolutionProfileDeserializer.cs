using Core.Startup;
using Models;
using Models.Events;
using Serilog;
using YamlDotNet.Serialization;

namespace Core;

public class SolutionProfileDeserializer : IStartUpProcess
{
    private const string solutionProfileFileName = ".darwingSolutionProfile.yml";

    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationSettings _mutationSettings;

    public SolutionProfileDeserializer(IEventAggregator eventAggregator, IMutationSettings mutationSettings)
    {
        ArgumentNullException.ThrowIfNull(eventAggregator);
        ArgumentNullException.ThrowIfNull(mutationSettings);

        _mutationSettings = mutationSettings;
        _eventAggregator = eventAggregator;
    }

    public void StartUp()
    {
        _eventAggregator.GetEvent<SolutionPathProvided>().Subscribe(x => LoadSlnProfileIfPresent(x.SolutionPath));
    }

    private void LoadSlnProfileIfPresent(string slnFilePath)
    {
        ArgumentNullException.ThrowIfNull(slnFilePath);

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
            Log.Error("Failed to deserialize the provieded solution profile data. Continuing without it.");
            Log.Debug($"Exception: {ex}");
        }
    }

    private void AssignSettingsFromProfile(SolutionProfileData profileData)
    {
        _mutationSettings.SolutionProfileData = profileData;

        //TODO: as more settings are introduced and used, will need to update them here.
        _mutationSettings.TestProjectNames = profileData.TestProjects;
    }
}