namespace Mutator;

public static class IEnumerableExtension
{
    /// <summary>
    /// None of the items in the list match the predicate.
    /// Inverse of any, to reduce negation and complexity
    /// </summary>
    public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool>? predicate = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (TSource element in source)
        {
            if (predicate is null || predicate(element))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Helper method for when there is a dictionary of lists. 
    /// If the key exists, it will add the new item to the list at that key, otherwise it will create a list at that key
    /// </summary>
    public static void AddOrCreate<TKey, TItem>(this Dictionary<TKey, List<TItem>> source, TKey key, TItem value) where TKey: notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        if (source.TryGetValue(key, out List<TItem>? existingList))
        {
            existingList.Add(value);
        }
        else
        {
            List<TItem> newList = [value];
            source.TryAdd(key, newList);
        }
    }
}