namespace TWSort.Sorters;
public class SorterAggregator()
{
    private readonly IEnumerable<Sorter> _sorters = [new HtmlSorter(), new CssSorter(), new JSSorter(), new RazorSorter()];

    public string Sort(string filePath, string fileContent)
    {
        // Use HtmlSorter as the default if no specific sorter is found for the file extension
        return _sorters.FirstOrDefault(g => g.Handled.Contains(Path.GetExtension(filePath)), _sorters.First()).Sort(filePath, fileContent);
    }
}