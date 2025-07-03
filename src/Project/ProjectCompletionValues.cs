using System.Data;

namespace TWSort.Project;
public class ProjectCompletionValues
{
    /// <summary>
    /// When set, set to lowercase
    /// </summary>
    public string FilePath { get; set; } = null!;
    public List<string> ApplicablePaths { get; set; } = [];
    /// <summary>
    /// V4+ only
    /// </summary>
    public List<string> NotApplicablePaths { get; set; } = [];

    public bool Initialized { get; set; }
    public List<TailwindClass> Classes { get; set; } = [];
    public List<string> Variants { get; set; } = [];
    public Dictionary<string, string> Breakpoints { get; set; } = [];
    public Dictionary<string, string> Containers { get; set; } = [];

    public string? Prefix { get; set; }

    public HashSet<string> Colors { get; set; } = [];
    public HashSet<string> Spacing { get; set; } = [];
    /// <summary>
    /// Removed in V4
    /// </summary>
    public Dictionary<string, List<string>> ConfigurationValueToClassStems { get; set; } = [];

    /// <summary>
    /// Removed in V4
    /// </summary>
    public Dictionary<string, HashSet<string>> CustomColors { get; set; } = [];
    /// <summary>
    /// Removed in V4
    /// </summary>
    public Dictionary<string, HashSet<string>> CustomSpacing { get; set; } = [];

    /// <summary>
    /// V4 only
    /// </summary>
    public Dictionary<string, string> CssVariables { get; set; } = [];

    public List<string> PluginClasses { get; set; } = [];
    public List<string> PluginVariants { get; set; } = [];

    public HashSet<string> Blocklist { get; set; } = [];

    public TailwindVersion Version { get; set; }

    /// <summary>
    /// Is the class in the blocklist?
    /// </summary>
    /// <param name="className">The class to check</param>
    public bool IsClassAllowed(string className)
    {
        if (Version == TailwindVersion.V3)
        {
            className = className.Trim('!').Split(':').Last();

            if (!string.IsNullOrWhiteSpace(Prefix))
            {
                if (className.StartsWith(Prefix))
                {
                    className = className.Substring(Prefix!.Length);
                }
                else if (className.StartsWith($"-{Prefix}"))
                {
                    className = className.Substring(Prefix!.Length + 1);
                }
            }
        }

        return !Blocklist.Contains(className);
    }

    public ProjectCompletionValues Copy()
    {
        return new ProjectCompletionValues
        {
            Initialized = Initialized,
            Classes = [.. Classes.Select(c => c)],
            Variants = [.. Variants],
            Breakpoints = Breakpoints.ToDictionary(p => p.Key, p => p.Value),
            Prefix = Prefix,
            Colors = [.. Colors],
            Spacing = [.. Spacing],
            ConfigurationValueToClassStems = ConfigurationValueToClassStems.ToDictionary(
                kvp => kvp.Key, kvp => kvp.Value.ToList()
            ),
            CustomColors = CustomColors.ToDictionary(
                kvp => kvp.Key, kvp => kvp.Value.ToHashSet()
            ),
            CustomSpacing = CustomSpacing.ToDictionary(
                kvp => kvp.Key, kvp => kvp.Value.ToHashSet()
            ),
            PluginClasses = [.. PluginClasses],
            PluginVariants = [.. PluginVariants],
            Blocklist = [.. Blocklist],
            Version = Version,
            CssVariables = CssVariables.ToDictionary(
                kvp => kvp.Key, kvp => kvp.Value
            )
        };
    }
}