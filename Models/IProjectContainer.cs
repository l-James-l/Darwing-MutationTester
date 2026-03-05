using Microsoft.CodeAnalysis;
using Models.Enums;

namespace Models;

public interface IProjectContainer
{
    /// <summary>
    /// Unique BuildAnalyzer assigned ID of the project
    /// </summary>
    ProjectId ID { get; }

    /// <summary>
    /// Name of the project
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Fully qualified path to the csproj file
    /// </summary>
    string CsprojFilePath { get; }

    /// <summary>
    /// Fully qualified path to the directory containing the csproj file
    /// </summary>
    string DirectoryPath { get; }

    /// <summary>
    /// The path where the dll is output
    /// </summary>
    string DllFilePath { get; }

    /// <summary>
    /// The directory which will contain the outputted binaries form a build
    /// </summary>
    string OutputDirectory { get; }

    /// <summary>
    /// How we will treat the project
    /// </summary>
    ProjectType ProjectType { get; set; }

    /// <summary>
    /// Collection of all files in the project
    /// </summary>
    public SourceCodeFileCollection FileCollection { get; }

    /// <summary>
    /// Helper method to get a compilation for the project to allow emitting a new dll
    /// </summary>
    Compilation? GetCompilation();

    /// <summary>
    /// After mutation, need to update the underlying project instance
    /// </summary>
    void UpdateFromMutatedProject(Project proj);
}
