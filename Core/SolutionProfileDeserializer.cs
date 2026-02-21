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
        // Where the profile setting is null, use the default from settings
        _mutationSettings.TestProjects = profileData.TestProjects ?? _mutationSettings.TestProjects;
        _mutationSettings.IgnoreProjects = profileData.IgnoreProjects ?? _mutationSettings.IgnoreProjects;
        _mutationSettings.SourceCodeProjects = profileData.SourceCodeProjects ?? _mutationSettings.SourceCodeProjects;
        _mutationSettings.DisabledMutationTypes = profileData.DisabledMutationTypes ?? _mutationSettings.DisabledMutationTypes;

        _mutationSettings.SingleMutantPerLine = profileData.SingleMutantPerLine ?? _mutationSettings.SingleMutantPerLine;
        _mutationSettings.BuildTimeout = profileData.BuildTimeout ?? _mutationSettings.BuildTimeout;
        _mutationSettings.TestRunTimeout = profileData.TestRunTimeout ?? _mutationSettings.TestRunTimeout;
        _mutationSettings.SkipTestingNoActiveMutants = profileData.SkipTestingNoActiveMutants ?? _mutationSettings.SkipTestingNoActiveMutants;
        _mutationSettings.UseAdvancedProjectTypeAnalysis = profileData.UseAdvancedProjectTypeAnalysis ?? _mutationSettings.UseAdvancedProjectTypeAnalysis;
        _mutationSettings.DefaultGitComparisonBranch = profileData.DefaultGitComparisonBranch ?? _mutationSettings.DefaultGitComparisonBranch;
        _mutationSettings.MutationTestTimeoutScaler = profileData.MutationTestTimeoutScaler ?? _mutationSettings.MutationTestTimeoutScaler;
        _mutationSettings.AiTestGenerationAdditionalInstructions = profileData.AiTestGenerationAdditionalInstructions ?? _mutationSettings.AiTestGenerationAdditionalInstructions;
    }
}
