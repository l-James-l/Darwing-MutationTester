namespace Models.Events;

public class SolutionPathProvidedEvent : PubSubEvent<SolutionPathProvidedPayload> { }

public class SolutionPathProvidedPayload
{
    public string SolutionPath { get; set; }

    public SolutionPathProvidedPayload(string solutionPath)
    {
        SolutionPath = solutionPath;
    }
}