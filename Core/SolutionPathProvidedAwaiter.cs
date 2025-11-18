using Buildalyzer;
using Core.IndustrialEstate;
using Core.Interfaces;
using Core.Startup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;
using Models.Events;
using Serilog;

namespace Core;

/// <summary>
/// This class will handle performing actions required as soon as a solution path has been provided.
/// This includes:
///     - Locating the solution, and initializing the solution container
///     - Checking for a solution profile
///     - Performing an initial build
/// </summary>
public class SolutionPathProvidedAwaiter : IStartUpProcess, ISolutionProvider
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IAnalyzerManagerFactory _analyzerManagerFactory;
    private readonly ISolutionProfileDeserializer _slnProfileDeserializer;
    private readonly IMutationSettings _mutationSettings;

    //By making the public property the interface, we can mock the solution in testing.
    public ISolutionContainer SolutionContiner => _solutionContainer ?? throw new InvalidOperationException("Attempted to retrieve a solution before one has been loaded.");
    private SolutionContainer? _solutionContainer;

    public bool IsAvailable => _solutionContainer != null;

    public SolutionPathProvidedAwaiter(IEventAggregator eventAggregator, IAnalyzerManagerFactory analyzerManagerFactory, 
        ISolutionProfileDeserializer slnProfileDeserializer, IMutationSettings mutationSettings)
    {
        ArgumentNullException.ThrowIfNull(eventAggregator);
        ArgumentNullException.ThrowIfNull(analyzerManagerFactory);
        ArgumentNullException.ThrowIfNull(slnProfileDeserializer);
        ArgumentNullException.ThrowIfNull(mutationSettings);

        _eventAggregator = eventAggregator;
        _analyzerManagerFactory = analyzerManagerFactory;
        _slnProfileDeserializer = slnProfileDeserializer;
        _mutationSettings = mutationSettings;
    }

    public void StartUp()
    {
        //By using the startup process, we ensure the DI container will construct this class at application start.
        //Thus ensuring the subscription is made.
        _eventAggregator.GetEvent<SolutionPathProvidedEvent>().Subscribe(OnSolutionPathProvided);
    }

    private void TryCreateManager(string path)
    {
        try
        {
            IAnalyzerManager analyzerManager = _analyzerManagerFactory.CreateAnalyzerManager(path);
            _solutionContainer = new SolutionContainer(analyzerManager);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load solution.");
            Log.Debug($"Failed to create AnalyzerManager for solution at location: {path}. {ex}");
        }
    }

    private void OnSolutionPathProvided(SolutionPathProvidedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (string.IsNullOrWhiteSpace(payload.SolutionPath) || !payload.SolutionPath.EndsWith(".sln") || !File.Exists(payload.SolutionPath))
        {
            Log.Error($"Solution file not found at location: {payload.SolutionPath}");
            _mutationSettings.SolutionPath = "";
            _solutionContainer = null;
        }
        else
        {
            TryCreateManager(payload.SolutionPath);
        }

        if (_solutionContainer is not null)
        {
            //Do this outside the try catch so that errors caught are only for loading the solution.
            //Deserializer shall handle its own exceptions.
            _slnProfileDeserializer.LoadSlnProfileIfPresent(payload.SolutionPath);
            _solutionContainer.FindTestProjects(_mutationSettings);
            DiscoverSourceCodeFiles();
        }

        // Always publish this at the end so that the most recent build info can be updated, including where the new solution path was invalid.
        _eventAggregator.GetEvent<RequestSolutionBuildEvent>().Publish();
    }

    private void DiscoverSourceCodeFiles()
    {
        ArgumentNullException.ThrowIfNull(_solutionContainer);

        EnumerationOptions enumerationOptions = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.System | FileAttributes.Hidden | FileAttributes.Compressed | FileAttributes.Temporary | FileAttributes.ReadOnly
        };

        foreach (IProjectContainer project in _solutionContainer.SolutionProjects)
        {
            Log.Information($"Loading source code files for {project.Name}");

            List<string> files = Directory.EnumerateFiles(project.DirectoryPath, "*.cs", enumerationOptions).ToList();
            files.RemoveAll(path => 
                path.StartsWith(Path.Combine(project.DirectoryPath, "obj")) ||
                path.StartsWith(Path.Combine(project.DirectoryPath, "bin")));

            foreach (string file in files)
            {
                Log.Information($"Discovered: {file}");
                SyntaxTree syntaxTree = GetSyntaxTree(file);

                _solutionContainer.Solution.GetDocumentId(syntaxTree);
                if (_solutionContainer.Solution.GetDocumentId(syntaxTree) is { } documentId)
                {
                    project.SyntaxTrees.Add(documentId, syntaxTree);
                }
                else if (_solutionContainer.Solution.GetDocumentIdsWithFilePath(syntaxTree.FilePath) is { Length: 1 } documentIds)
                {
                    project.SyntaxTrees.Add(documentIds.First(), syntaxTree);
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
