using Spectre.Console;
using TWSort.Files;
using TWSort.Project.Configuration;

namespace TWSort.Project;

/// <summary>
/// Check ConfigurationClassGenerator.cs for the other half
/// </summary>
public partial class CompletionConfiguration(ProjectConfigurationManager projectConfigurationManager)
{
    private ProjectConfigurationManager ProjectConfigurationManager { get; } = projectConfigurationManager;

    /// <summary>
    /// Adjusts classes to match a change in the configuration file
    /// </summary>
    public async Task ReloadCustomAttributesAsync(string configurationFile)
    {
        try
        {
            await ReloadCustomAttributesImplAsync(configurationFile);
        }
        catch (Exception ex)
        {
            ex.Log();
        }
    }

    /// <summary>
    /// Implementation for <see cref="ReloadCustomAttributesAsync(ConfigurationFile)"/>
    /// </summary>
    private async Task ReloadCustomAttributesImplAsync(string configurationFile)
    {
        var version = await DirectoryVersionFinder.GetTailwindVersionAsync(configurationFile);

        var config = await ConfigFileParser.GetConfigurationAsync(configurationFile, version);

        var projectCompletionValues = ProjectConfigurationManager.GetCompletionConfigurationByConfigFilePath(configurationFile);

        projectCompletionValues.ApplicablePaths = [.. config.ContentPaths.Where(c => !c.StartsWith('!'))];
        projectCompletionValues.NotApplicablePaths = [.. config.ContentPaths.Where(c => c.StartsWith('!')).Select(c => c.Trim('!'))];

        if (version >= TailwindVersion.V4 && !string.IsNullOrWhiteSpace(config.Prefix))
        {
            projectCompletionValues.Prefix = $"{config.Prefix}:";
        }
        else
        {
            projectCompletionValues.Prefix = config.Prefix;
        }
        await LoadGlobalConfiguration(projectCompletionValues, config);
        projectCompletionValues.Variants = [.. projectCompletionValues.Variants.Distinct()];

        await LoadIndividualConfigurationOverride(projectCompletionValues, config);
        LoadIndividualConfigurationExtend(projectCompletionValues, config);

        LoadPlugins(projectCompletionValues, config);
    }

    private static bool GetDictionary(object value, out Dictionary<string, object> dict)
    {
        if (value is Dictionary<string, object> values)
        {
            dict = values;
            return true;
        }
        dict = [];
        return false;
    }
}
