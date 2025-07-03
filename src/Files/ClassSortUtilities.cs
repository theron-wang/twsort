using System.Text.Json;

namespace TWSort.Files;

public static class ClassSortUtilities
{
    private static readonly string _baseFolder = Path.Combine(AppContext.BaseDirectory, "Resources");

    private static readonly Dictionary<TailwindVersion, Dictionary<string, int>> _classOrders = [];
    private static readonly Dictionary<TailwindVersion, Dictionary<string, int>> _variantOrders = [];

    public static Dictionary<string, int> GetClassOrder(TailwindVersion version)
    {
        if (_classOrders.TryGetValue(version, out var value))
        {
            return value;
        }

        using (var fs = File.Open(Path.Combine(_baseFolder, version.ToString(), "order.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var classOrder = JsonSerializer.Deserialize<List<string>>(fs);
            _classOrders[version] = [];
            for (int i = 0; i < classOrder!.Count; i++)
            {
                _classOrders[version][classOrder[i]] = i;
            }
        }

        return _classOrders[version];
    }

    public static Dictionary<string, int> GetVariantOrder(TailwindVersion version)
    {
        if (_variantOrders.TryGetValue(version, out var value))
        {
            return value;
        }

        using (var fs = File.Open(Path.Combine(_baseFolder, version.ToString(), "variantorder.json"), FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var variantOrder = JsonSerializer.Deserialize<List<string>>(fs);
            _variantOrders[version] = [];
            for (int i = 0; i < variantOrder!.Count; i++)
            {
                // Multiply by 100 so that containers/breakpoints have flexibility
                _variantOrders[version][variantOrder[i]] = i * 100;
            }
        }

        return _variantOrders[version];
    }
}
