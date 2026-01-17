using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Models;
using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using Serilog;

namespace Mutator;

public class MutatedProjectBuilder : IMutatedProjectBuilder
{
    private readonly IMutationDiscoveryManager _mutationDiscovery;
    private readonly IEventAggregator _eventAggregator;
    private readonly ISolutionProvider _solutionProvider;
    private readonly IStatusTracker _statusTracker;
    private readonly IMutatedSolutionTester _mutatedSolutionTester;

    public MutatedProjectBuilder(IMutationDiscoveryManager mutationDiscovery, IEventAggregator eventAggregator,
        ISolutionProvider solutionProvider, IStatusTracker statusTracker, IMutatedSolutionTester mutatedSolutionTester)
    {
        ArgumentNullException.ThrowIfNull(mutationDiscovery);
        ArgumentNullException.ThrowIfNull(eventAggregator);
        ArgumentNullException.ThrowIfNull(solutionProvider);
        ArgumentNullException.ThrowIfNull(statusTracker);
        ArgumentNullException.ThrowIfNull(mutatedSolutionTester);

        _mutationDiscovery = mutationDiscovery;
        _eventAggregator = eventAggregator;
        _solutionProvider = solutionProvider;
        _statusTracker = statusTracker;
        _mutatedSolutionTester = mutatedSolutionTester;
    }

    public void Build()
    {
        if (!_statusTracker.TryStartOperation(DarwingOperation.BuildingMutatedSolution))
        {
            return;
        }

        bool allProjectsBuilt = false;
        try
        {
            allProjectsBuilt = EmitAllChanges();
        }
        catch
        {
            Log.Error("An unexpected error occurred while building mutated solution.");
            allProjectsBuilt = false;
        }

        RestoreDependencies();
        _statusTracker.FinishOperation(DarwingOperation.BuildingMutatedSolution, allProjectsBuilt);
        _mutatedSolutionTester.RunTestsOnMutatedSolution();
    }

    private bool EmitAllChanges()
    {
        const int maxRetries = 5;
        bool allProjectsBuilt = true;
        foreach (IProjectContainer project in _solutionProvider.SolutionContainer.SolutionProjects)
        {
            int retryCount = 0;
            bool doFinalRetry = false;
            bool dllCreated = false;
            while (retryCount < maxRetries && !doFinalRetry)
            {
                retryCount++;
                dllCreated = EmitMutatedDll(project, out List<Diagnostic> failures);
                if (!dllCreated)
                {
                    bool anyFailuresActioned = FindAndRemoveMutationsCausingFailures(project, failures);
                    if (!anyFailuresActioned)
                    {
                        Log.Warning("Build encountered errors that could not be resolved. Will make 1 final attempt.");
                        doFinalRetry = true;
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            if (!dllCreated && doFinalRetry)
            {
                if (EmitMutatedDll(project, out List<Diagnostic> failures))
                {
                    Log.Information("Final attempt to create DLL for {proj} succeeded.", project.Name);
                }
                else
                {
                    allProjectsBuilt = false;
                    Log.Error("Final attempt to created mutated DLL for {proj} failed. Will be unable to perform mutation testing. Examine logs for details.", project.Name);
                    break;
                }
            }
            else if (!dllCreated)
            {
                allProjectsBuilt = false;
                Log.Error("Max number of retries reached for creating a mutated DLL for {proj}. Will be unable to perform mutation testing.", project.Name);
                break;
            }
        }

        return allProjectsBuilt;
    }

    private bool EmitMutatedDll(IProjectContainer mutatedProject, out List<Diagnostic> failures)
    {
        Log.Information($"Attempting to emit dll for {mutatedProject.Name}");

        failures = [];

        Compilation? compilation = mutatedProject.GetCompilation();
        if (compilation == null)
        {
            Log.Error($"Failed to created compilation for mutated project {mutatedProject.Name}.");
            return false;
        }

        EmitResult emitResult = compilation.Emit(mutatedProject.DllFilePath);
        if (!emitResult.Success)
        {
            Log.Error("Emitting dll for {Name} failed. {Diagnostics}.", mutatedProject.Name, emitResult.Diagnostics);
            failures = emitResult.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).ToList();
            return false;
        }

        Log.Information($"Mutated dll successfully created for {mutatedProject.Name}.");
        return true;
    }

    private void RestoreDependencies()
    {
        // For every project that has a dependency on another project that has been mutated, we need to replace the dll in that project.
        foreach (IProjectContainer project in _solutionProvider.SolutionContainer.AllProjects)
        {
            Log.Information("Restoring dependencies for {project}.", project.Name);

            // Get the folder containing the projects build artifacts, which will include dll's of any other projects.
            string projectOutputDirectory = Path.GetDirectoryName(project.DllFilePath) ?? "";

            foreach (IProjectContainer mutatedProject in _solutionProvider.SolutionContainer.SolutionProjects.Except([project]))
            {
                // Check if the unmutated dll exists in the output directory, and if it does, replace it
                string wouldBeDependency = Path.Combine(projectOutputDirectory, Path.GetFileName(mutatedProject.DllFilePath));
                if (File.Exists(wouldBeDependency))
                {
                    Log.Debug($"Replacing dependency on {mutatedProject.Name} in {project.Name}.");
                    File.Copy(mutatedProject.DllFilePath, wouldBeDependency, true);
                }
            }
        }
    }

    /// <summary>
    /// For each failure in the build diagnostics, we will remove the mutations which was introduced closest to that location.
    /// </summary>
    /// <returns>True if all failures were actioned, False if any failures were unable to be actioned.</returns>
    private bool FindAndRemoveMutationsCausingFailures(IProjectContainer project, List<Diagnostic> failures)
    {
        bool anyActioned = false;
        foreach (Diagnostic failure in failures)
        {
            if (!failure.Location.IsInSource || failure.Location.SourceSpan.IsEmpty)
            {
                Log.Error("failure not located in source code so cannot determine a mutation that caused it. {failure}", failure);
                continue;
            }

            FileLinePositionSpan failureLocation = failure.Location.GetLineSpan();
            if (project.DocumentsByPath.TryGetValue(failureLocation.Path, out DocumentId? document))
            {
                Log.Information("Attempting to address a build failure in file: {path}", failureLocation.Path);

                IEnumerable<DiscoveredMutation> mutationsInFile = _mutationDiscovery.DiscoveredMutations.Where(x => x.Document == document && x.Status >= MutantStatus.Available);

                //We want to find any mutation where the error occurs partially or entirely inside it
                //We do this by finding all mutations which 'start' before the error ends,
                //then find all the mutations which 'end' after the error starts
                //Then we take the intersection of these, leaving us with mutations that overlap with the error
                IEnumerable<DiscoveredMutation> mutationStartBeforeErrorEnd = 
                    mutationsInFile.Where(mutant => mutant.LineSpan.StartLinePosition.CompareTo(failureLocation.Span.End) <= 0);
                IEnumerable<DiscoveredMutation> mutationsEndAfterErrorStart = 
                    mutationsInFile.Where(x => x.LineSpan.EndLinePosition.CompareTo(failureLocation.Span.Start) >= 0);

                IEnumerable<DiscoveredMutation> mutantsToRemove = mutationStartBeforeErrorEnd.Intersect(mutationsEndAfterErrorStart);

                RemoveMutants(mutantsToRemove.ToList());
                anyActioned = true;

                //TODO maybe it would be better to not remove all the mutants at once,
                //and instead remove the most relevant one first (smallest span entirely inside?), and then if that fails, remove them all?
            }
            else
            {
                Log.Warning("No document found that matches that path specified in the build failure. {failure}", failureLocation.Path);
                continue;
            }
        }

        return anyActioned;
    }

    private void RemoveMutants(List<DiscoveredMutation> mutantsToRemove)
    {
        Solution slnWithMutantsRemoved = _solutionProvider.SolutionContainer.Solution;

        while (mutantsToRemove.Count > 0)
        {
            //Take the first mutation which is not contained within another mutation we need to remove.
            DiscoveredMutation? mutant = mutantsToRemove.FirstOrDefault(x => mutantsToRemove.Except([x]).None(y => y.LineSpan.Contains(x.LineSpan)));

            if (mutant is null)
            {
                Log.Warning("Attempted to remove a mutation that couldn't be found");
                mutant = mutantsToRemove.First();
            }
            mutantsToRemove.Remove(mutant);
            foreach (DiscoveredMutation embeddedMutation in new List<DiscoveredMutation>(mutantsToRemove.Where(x => mutant.LineSpan.Contains(x.LineSpan))))
            {
                //Removing a mutation that contains this one will by default remove this mutant.
                embeddedMutation.Status = MutantStatus.CausedBuildError;
                mutantsToRemove.Remove(embeddedMutation);
            }

            mutant.Status = MutantStatus.CausedBuildError;

            SyntaxNode newRoot = mutant.MutatedNode.SyntaxTree.GetRoot().ReplaceNode(mutant.MutatedNode, mutant.OriginalNode);
            
            slnWithMutantsRemoved = slnWithMutantsRemoved.WithDocumentSyntaxRoot(mutant.Document, newRoot);

            _mutationDiscovery.RediscoverMutationsInTree(newRoot);

            Log.Debug(mutant.OriginalNode.SyntaxTree.FilePath + " after removing mutants causing build errors:");
            Log.Debug(newRoot.ToFullString());
        }


        if (!_solutionProvider.SolutionContainer.Workspace.TryApplyChanges(slnWithMutantsRemoved))
        {
            Log.Error("Failed to remove mutants causing errors.");
        }
        _solutionProvider.SolutionContainer.RestoreProjects();
    }
}
