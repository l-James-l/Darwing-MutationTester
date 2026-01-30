using System.Collections.ObjectModel;

namespace GUI.Services;

public static class ObservableCollectionExtensions
{
    public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> nodes)
    {
        foreach (T node in nodes)
        {
            collection.Add(node);
        }
    }

    public static void RemoveWhere<T>(this ObservableCollection<T> collection, Predicate<T> predicate)
    {
        List<T> toRemove = [.. collection.Where(predicate.Invoke)];
        foreach (T node in toRemove)
        {
            collection.Remove(node);
        }
    }
}