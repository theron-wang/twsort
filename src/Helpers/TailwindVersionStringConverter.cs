namespace TWSort.Helpers;
public static class TailwindVersionStringConverter
{
    public static string ToFormattedString(this TailwindVersion version)
    {
        if (version == TailwindVersion.V4_1)
        {
            return "4.1.x";
        }
        if (version == TailwindVersion.V4)
        {
            return "4.0.x";
        }
        return "3.0.x";
    }
}
