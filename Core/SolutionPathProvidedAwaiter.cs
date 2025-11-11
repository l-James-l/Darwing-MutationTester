using Buildalyzer;
using Core.IndustrialEstate;
using Core.Startup;
using Models;
using Models.Events;
using Serilog;

namespace Core;

public class SolutionPathProvidedAwaiter : IStartUpProcess, ISolutionProvider
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IAnalyzerManagerFactory _analyzerManagerFactory;

    public SolutionContiner SolutionContiner => _solutionContainer ?? throw new InvalidOperationException("Attempted to retrieve a solution before one has been loaded.");
    private SolutionContiner? _solutionContainer;

    public SolutionPathProvidedAwaiter(IEventAggregator eventAggregator, IAnalyzerManagerFactory analyzerManagerFactory)
    {
        ArgumentNullException.ThrowIfNull(eventAggregator);
        ArgumentNullException.ThrowIfNull(analyzerManagerFactory);

        _eventAggregator = eventAggregator;
        _analyzerManagerFactory = analyzerManagerFactory;
    }

    public void StartUp()
    {
        //By using the startup process, we ensure the DI container will construct this class at application start.
        //Thus ensuring the subscription is made.
        _eventAggregator.GetEvent<SolutionPathProvided>().Subscribe(OnSolutionPathProvided);
    }

    private void OnSolutionPathProvided(SolutionPathProvidedPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (string.IsNullOrWhiteSpace(payload.SolutionPath) || !payload.SolutionPath.EndsWith(".sln") || !File.Exists(payload.SolutionPath))
        {
            Log.Error($"Solution file not found at location: {payload.SolutionPath}");
            return;
        }

        try
        {
            IAnalyzerManager analyzerManager = _analyzerManagerFactory.CreateAnalyzerManager(payload.SolutionPath);
            _solutionContainer = new SolutionContiner(analyzerManager);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load solution.");
            Log.Debug($"Failed to create AnalyzerManager for solution at location: {payload.SolutionPath}. {ex}");
            return;
        }
    }
}
