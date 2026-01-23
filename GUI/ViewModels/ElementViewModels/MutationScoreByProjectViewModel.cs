using Microsoft.CodeAnalysis;
using Models;
using Models.Enums;
using Models.Events;
using Models.Exceptions;
using Mutator;
using Serilog;
using System.Collections.ObjectModel;

namespace GUI.ViewModels.ElementViewModels;

/// <summary>
/// ViewModel for the project summaries section on the dashboard.
/// </summary>
public class MutationScoreByProjectViewModel
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationDiscoveryManager _mutationDiscoveryManager;
    private readonly ISolutionProvider _slnProvider;

    public ObservableCollection<IndividualProjectSummaryViewModel> Projects { get; private set; } = new();

    public MutationScoreByProjectViewModel(IEventAggregator eventAggregator, IMutationDiscoveryManager mutationDiscoveryManager,
        ISolutionProvider slnProvider)
    {
        _eventAggregator = eventAggregator;
        _mutationDiscoveryManager = mutationDiscoveryManager;
        _slnProvider = slnProvider;

        _eventAggregator.GetEvent<MutationUpdated>().Subscribe(OnMutationUpdated, ThreadOption.UIThread);
        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Subscribe(OnSolutionLoaded, ThreadOption.UIThread, true, x => x is DarwingOperation.LoadSolution);
    }

    private void OnSolutionLoaded(DarwingOperation operation)
    {
        Projects.Clear();
        if (!_slnProvider.IsAvailable)
        {
            // User tried to load a new solution which failed. nothing more to show.
            return;
        }

        // For the new sln, create a bunch of empty project views. No mutations should have been discovered yet, but
        // in case there are any left over from the previous run, don't try and populate based on what is available.
        foreach (IProjectContainer proj in _slnProvider.SolutionContainer.SolutionProjects)
        {
            Projects.Add(new IndividualProjectSummaryViewModel(proj));
        }
    }

    private void OnMutationUpdated(SyntaxAnnotation id)
    {
        //Important: when a mutation is first created, the model doesn't know where it is, and it will need to be rediscovered later
        //For that reason when accessing the document, this needs to be done in a try catch
        try
        {
            UpdateDisplayedMutation(id);
        }
        catch (PropertyNotAssignedException ex)
        {
            Log.Information(ex, "Mutation updated could not be processed because not all of its required properties have been initialised yet.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Unexpected error occurred while trying to display a mutation.");
        }
    }

    private void UpdateDisplayedMutation(SyntaxAnnotation id)
    {
        if (_mutationDiscoveryManager.DiscoveredMutations.FirstOrDefault(x => x.ID == id) is not DiscoveredMutation mutation)
        {
            Log.Error("Received update for a mutation ID not present in the list of discovered mutations");
            return;
        }

        if (Projects.FirstOrDefault(x => x.ID == mutation.Document.ProjectId) is IndividualProjectSummaryViewModel proj)
        {
            IEnumerable<DiscoveredMutation> projMutations = _mutationDiscoveryManager.DiscoveredMutations.Where(x => x.Document.ProjectId == proj.ID);
            proj.TotalMutations = projMutations.Count(x => x.Status is not MutantStatus.CausedBuildError);
            proj.KilledMutations = projMutations.Count(x => x.Status is MutantStatus.Killed);
            proj.SurvivedMutations = projMutations.Count(x => x.Status is MutantStatus.Survived);
        }
    }
}
