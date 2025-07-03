namespace TWSort.Helpers;
internal static class TrimPrefixHelper
{
    public static string TrimPrefix(this string s, string prefix, StringComparison comparison)
    {
        if (s.StartsWith(prefix, comparison))
        {
            return s[prefix.Length..];
        }

        return s;
    }
}
