using Buildalyzer;
using Core.IndustrialEstate;
using Core.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;
using Models.Enums;
using Models.SharedInterfaces;
using Serilog;
using System.IO.Abstractions;

namespace Core;

/// <summary>
/// This class will handle performing actions required as soon as a solution path has been provided.
/// This includes:
///     - Locating the solution, and initializing the solution container
///     - Checking for a solution profile
///     - Finding source code files in each project and adding them to the project containers
///     - Finding test projects
///     - Triggering an initial build of the solution
///     
/// </summary>
public class SolutionLoader : ISolutionLoader
{
    private readonly IAnalyzerManagerFactory _analyzerManagerFactory;
    private readonly ISolutionProfileDeserializer _slnProfileDeserializer;
    private readonly IMutationSettings _mutationSettings;
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly IStatusTracker _statusTracker;
    private readonly ISolutionProvider _solutionProvider;
    private readonly IGitDiffManager _gitManager;
    private readonly IFileSystem _fileSystem;

    public SolutionLoader(IAnalyzerManagerFactory analyzerManagerFactory, ISolutionProfileDeserializer slnProfileDeserializer,
        IMutationSettings mutationSettings, ISolutionBuilder solutionBuilder, IStatusTracker statusTracker,
        ISolutionProvider solutionProvider, IGitDiffManager gitManager, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(analyzerManagerFactory);
        ArgumentNullException.ThrowIfNull(slnProfileDeserializer);
        ArgumentNullException.ThrowIfNull(mutationSettings);
        ArgumentNullException.ThrowIfNull(solutionBuilder);
        ArgumentNullException.ThrowIfNull(solutionProvider);
        ArgumentNullException.ThrowIfNull(statusTracker);
        ArgumentNullException.ThrowIfNull(gitManager);

        _analyzerManagerFactory = analyzerManagerFactory;
        _slnProfileDeserializer = slnProfileDeserializer;
        _mutationSettings = mutationSettings;
        _solutionBuilder = solutionBuilder;
        _statusTracker = statusTracker;
        _solutionProvider = solutionProvider;
        _gitManager = gitManager;
        _fileSystem = fileSystem;
    }

    public void Load(string solutionPath)
    {
        ArgumentNullException.ThrowIfNull(solutionPath);

        Log.Information("Received solution path: {path}", solutionPath);

        if (!_statusTracker.TryStartOperation(DarwingOperation.LoadSolution))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(solutionPath) || !solutionPath.EndsWith(".sln") || !File.Exists(solutionPath))
        {
            Log.Error($"Solution file not found at location: {solutionPath}");
            _mutationSettings.SolutionPath = "";
            _statusTracker.FinishOperation(DarwingOperation.LoadSolution, false);
            return;
        }
        
        _mutationSettings.SolutionPath = solutionPath;
        //Need to load the profile before creating the SolutionContainer so that overridden project types are assigned correctly
        _slnProfileDeserializer.LoadSlnProfileIfPresent(solutionPath);

        SolutionContainer? solutionContainer = TryCreateManager(solutionPath);
        

        if (solutionContainer is not null)
        {
            //Do this outside the try catch so that errors caught are only for loading the solution.
            DiscoverSourceCodeFiles(solutionContainer);
            _solutionProvider.NewSolution(solutionContainer);
            _statusTracker.FinishOperation(DarwingOperation.LoadSolution, true);
            _gitManager.InitialGitDiff();

            _solutionBuilder.InitialBuild();
        }
        else
        {
            _statusTracker.FinishOperation(DarwingOperation.LoadSolution, false);
        }
    }

    private SolutionContainer? TryCreateManager(string path)
    {
        try
        {
            Log.Information("Creating analyzer for solution.");
            IAnalyzerManager analyzerManager = _analyzerManagerFactory.CreateAnalyzerManager(path);
            return new SolutionContainer(analyzerManager, _mutationSettings);
        }
        catch (Exception ex)
        {
            _mutationSettings.SolutionPath = "";
            Log.Error("Failed to load solution.");
            Log.Debug($"Failed to create AnalyzerManager for solution at location: {path}. {ex}");
            return null;
        }
    }

    private void DiscoverSourceCodeFiles(SolutionContainer solutionContainer)
    {
        EnumerationOptions enumerationOptions = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.System | FileAttributes.Hidden | FileAttributes.Compressed | FileAttributes.Temporary | FileAttributes.ReadOnly
        };

        foreach (IProjectContainer project in solutionContainer.SolutionProjects)
        {
            Log.Information($"Loading source code files for {project.Name}");

            List<string> files = _fileSystem.Directory.EnumerateFiles(project.DirectoryPath, "*.cs", enumerationOptions).ToList();
            files.RemoveAll(path => 
                path.StartsWith(Path.Combine(project.DirectoryPath, "obj")) ||
                path.StartsWith(Path.Combine(project.DirectoryPath, "bin")));

            foreach (string file in files)
            {
                Log.Information($"Discovered: {file}");
                SyntaxTree syntaxTree = GetSyntaxTree(file);

                if (solutionContainer.Solution.GetDocumentId(syntaxTree) is { } documentId)
                {
                    project.FileCollection.AddDocument(documentId, syntaxTree);
                }
                else if (solutionContainer.Solution.GetDocumentIdsWithFilePath(syntaxTree.FilePath) is { Length: 1 } documentIds)
                {
                    project.FileCollection.AddDocument(documentIds.First(), syntaxTree);
                }
                else
                {
                    Log.Error($"Unable to find document ID for {syntaxTree.FilePath}. This file will not be mutated.");
                }
            }
        }
    }

    private SyntaxTree GetSyntaxTree(string path)
    {
        string code = File.ReadAllText(path);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        return tree.WithFilePath(path);
    }
}
