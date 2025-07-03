namespace TWSort.Helpers;
public static class HashSetAddRange
{
    public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(hashSet);
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            hashSet.Add(item);
        }
    }
}
