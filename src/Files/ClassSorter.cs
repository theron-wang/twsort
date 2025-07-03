using System.Text;
using TWSort.Sorters;

namespace TWSort.Files;

public static class ClassSorter
{
    private static readonly SorterAggregator _sorter = new();

    public static async Task SortAsync(string path)
    {
        string fileContent;
        Encoding encoding;

        using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using var reader = new StreamReader(file);
            encoding = reader.CurrentEncoding;
            fileContent = await reader.ReadToEndAsync();
        }

        var sorted = _sorter.Sort(path, fileContent);

        if (sorted != fileContent)
        {
            using var file = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(file, encoding);
            await writer.WriteAsync(sorted);
        }
    }
}
