namespace Mutator;

public static class IEnumerableExtension
{
    public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (TSource element in source)
        {
            if (predicate(element))
            {
                return false;
            }
        }

        return true;
    }
}