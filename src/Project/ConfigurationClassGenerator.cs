using System.Globalization;
using TailwindCSSIntellisense.Configuration;

namespace TWSort.Project;

public partial class CompletionConfiguration
{
    /// <summary>
    /// Reconfigures colors, spacing, and screen as well as any non-theme properties (prefix, blocklist, etc.)
    /// </summary>
    private async Task LoadGlobalConfiguration(ProjectCompletionValues project, TailwindConfiguration config)
    {
        var original = await ProjectConfigurationManager.GetUnsetCompletionConfiguration(project.Version);

        project.Spacing = [.. original.Spacing];
        project.Breakpoints = original.Breakpoints.ToDictionary(p => p.Key, p => p.Value);
        project.Containers = original.Containers.ToDictionary(p => p.Key, p => p.Value);
        project.Colors = [.. original.Colors];
        project.Blocklist = [.. config?.Blocklist ?? []];
        project.CssVariables = original.CssVariables.ToDictionary(p => p.Key, p => p.Value);

        if (config is null)
        {
            // Reset to default; either user has changed/deleted config file or there is none
            return;
        }

        if (config.OverridenValues.TryGetValue("colors", out var value) && GetDictionary(value, out var dict))
        {
            project.Colors = GetColors(dict, project.Version);
        }
        if (config.ExtendedValues.TryGetValue("colors", out value) && GetDictionary(value, out dict))
        {
            project.Colors.AddRange(GetColors(dict, project.Version));
        }

        if (config.OverridenValues.TryGetValue("screens", out value) && GetDictionary(value, out dict))
        {
            project.Breakpoints = [];

            foreach (var pair in dict)
            {
                project.Breakpoints[pair.Key] = pair.Value.ToString()!;
            }
        }

        if (config.ExtendedValues.TryGetValue("screens", out value) && GetDictionary(value, out dict))
        {
            foreach (var pair in dict)
            {
                project.Breakpoints[pair.Key] = pair.Value.ToString()!;
            }
        }

        if (config.OverridenValues.TryGetValue("v4-container", out value) && GetDictionary(value, out dict))
        {
            project.Containers = [];

            foreach (var pair in dict)
            {
                project.Containers[pair.Key] = pair.Value.ToString()!;
            }
        }
        if (config.ExtendedValues.TryGetValue("v4-container", out value) && GetDictionary(value, out dict))
        {
            foreach (var pair in dict)
            {
                project.Containers[pair.Key] = pair.Value.ToString()!;
            }
        }

        if (config.OverridenValues.TryGetValue("spacing", out value) && GetDictionary(value, out dict))
        {
            project.Spacing = [.. dict.Keys];
        }
        if (config.ExtendedValues.TryGetValue("spacing", out value) && GetDictionary(value, out dict))
        {
            project.Spacing.AddRange(dict.Keys);
        }

        if (project.Version >= TailwindVersion.V4)
        {
            DictionaryHelpers.MergeDictionaries(config.ThemeVariables, project.CssVariables);
            project.CssVariables = config.ThemeVariables;
        }
    }

    /// <summary>
    /// If config.EnabledCorePlugins is not null, all classes will be disabled except
    /// for those in enabled core plugins. If config.DisabledCorePlugins is not null and empty,
    /// all classes will exist except for those explicitly disabled.
    /// </summary>
    private async Task HandleCorePlugins(ProjectCompletionValues project, TailwindConfiguration config)
    {
        var original = await ProjectConfigurationManager.GetUnsetCompletionConfiguration(project.Version);
        var enabledClasses = new List<TailwindClass>();
        if (config.EnabledCorePlugins is not null)
        {
            foreach (var plugin in config.EnabledCorePlugins)
            {
                if (project.ConfigurationValueToClassStems.ContainsKey(plugin))
                {
                    var stems = project.ConfigurationValueToClassStems[plugin];

                    foreach (var stem in stems)
                    {
                        if (stem.Contains("{*}"))
                        {
                            var s = stem.Replace("-{*}", "");

                            enabledClasses.AddRange(original.Classes.Where(c => c.Name.StartsWith(s) && c.UseColors == false && c.UseSpacing == false));
                        }
                        else if (stem.Contains("{s}"))
                        {
                            var s = stem.Replace("-{s}", "");

                            enabledClasses.AddRange(original.Classes.Where(c => c.Name.StartsWith(s) && c.UseColors == false && c.UseSpacing));
                        }
                        else if (stem.Contains("{c}"))
                        {
                            var s = stem.Replace("-{c}", "");

                            enabledClasses.AddRange(original.Classes.Where(c => c.Name.StartsWith(s) && c.UseColors && c.UseSpacing == false));
                        }
                        else if (stem.Contains('{'))
                        {
                            var s = stem.Replace($"-{stem.Split('-').Last()}", "");
                            var values = stem.Split('-').Last().Trim('{', '}').Split('|');

                            bool negate = false;
                            if (values[0].StartsWith("!"))
                            {
                                negate = true;
                                values[0] = values[0].Trim('!');
                            }

                            var classes = values.Select(v => $"{s}-{v}");

                            if (negate)
                            {
                                enabledClasses.AddRange(original.Classes.Where(c => c.Name.StartsWith(s) && classes.Contains(c.Name) == false && c.UseColors == false && c.UseSpacing == false));
                            }
                            else
                            {
                                enabledClasses.AddRange(original.Classes.Where(c => classes.Contains(c.Name) && c.UseColors == false && c.UseSpacing == false));
                            }
                        }
                        else
                        {
                            enabledClasses.AddRange(original.Classes.Where(c => c.Name.StartsWith(stem) && c.Name.Replace($"{stem}-", "").Count(ch => ch == '-') == 0 && c.UseColors == false && c.UseSpacing == false));
                        }
                    }
                }
            }
        }
        else
        {
            enabledClasses = [.. original.Classes];

            if (config.DisabledCorePlugins is not null && config.DisabledCorePlugins.Count > 0)
            {
                foreach (var plugin in config.DisabledCorePlugins)
                {
                    if (project.ConfigurationValueToClassStems.ContainsKey(plugin))
                    {
                        var stems = project.ConfigurationValueToClassStems[plugin];

                        foreach (var stem in stems)
                        {
                            if (stem.Contains("{*}"))
                            {
                                var s = stem.Replace("-{*}", "");

                                enabledClasses.RemoveAll(c => c.Name.StartsWith(s) && c.UseColors == false && c.UseSpacing == false);
                            }
                            else if (stem.Contains("{s}"))
                            {
                                var s = stem.Replace("-{s}", "");

                                enabledClasses.RemoveAll(c => c.Name.StartsWith(s) && c.UseColors == false && c.UseSpacing);
                            }
                            else if (stem.Contains("{c}"))
                            {
                                var s = stem.Replace("-{c}", "");

                                enabledClasses.RemoveAll(c => c.Name.StartsWith(s) && c.UseColors && c.UseSpacing == false);
                            }
                            else if (stem.Contains('{'))
                            {
                                var s = stem.Replace($"-{stem.Split('-').Last()}", "");
                                var values = stem.Split('-').Last().Trim('{', '}').Split('|');

                                bool negate = false;
                                if (values[0].StartsWith("!"))
                                {
                                    negate = true;
                                    values[0] = values[0].Trim('!');
                                }

                                var classes = values.Select(v => $"{s}-{v}");

                                if (negate)
                                {
                                    enabledClasses.RemoveAll(c => c.Name.StartsWith(s) && classes.Contains(c.Name) == false && c.UseColors == false && c.UseSpacing == false);
                                }
                                else
                                {
                                    enabledClasses.RemoveAll(c => classes.Contains(c.Name) && c.UseColors == false && c.UseSpacing == false);
                                }
                            }
                            else
                            {
                                enabledClasses.RemoveAll(c => c.Name.StartsWith(stem) && c.Name.Replace($"{stem}-", "").Count(ch => ch == '-') == 0 && c.UseColors == false && c.UseSpacing == false);
                            }
                        }
                    }
                }
            }
        }

        project.Classes = enabledClasses;
    }

    /// <summary>
    /// Reconfigures all classes based on the specified configuration (configures theme.____)
    /// </summary>
    /// <param name="config">The configuration object</param>
    private async Task LoadIndividualConfigurationOverride(ProjectCompletionValues project, TailwindConfiguration config)
    {
        if (config is null)
        {
            return;
        }

        await HandleCorePlugins(project, config);

        var original = await ProjectConfigurationManager.GetUnsetCompletionConfiguration(project.Version);

        var applicable = project.ConfigurationValueToClassStems.Keys.Where(k => config.OverridenValues?.ContainsKey(k) == true);
        project.Variants = [.. original.Variants];
        var classesToRemove = new List<TailwindClass>();
        var classesToAdd = new List<TailwindClass>();

        project.CustomSpacing = [];
        project.CustomColors = [];

        foreach (var key in applicable)
        {
            var stems = project.ConfigurationValueToClassStems[key];

            foreach (var stem in stems)
            {
                if (stem.Contains(':'))
                {
                    var s = stem.Trim(':');
                    project.Variants.RemoveAll(c => c.StartsWith(s) && c.Replace($"{s}-", "").Count(ch => ch == '-') == 0 && c.Contains("[]") == false);

                    if (GetDictionary(config.OverridenValues[key], out var dict))
                    {
                        project.Variants.AddRange(dict.Keys.Select(k => k == "DEFAULT" ? s : $"{s}-{k}"));
                    }
                }
                else if (stem.Contains("{s}"))
                {
                    if (GetDictionary(config.OverridenValues[key], out var dict))
                    {
                        project.CustomSpacing[stem.Replace("{s}", "{0}")] = [.. dict.Select(p => p.Key == "DEFAULT" ? "" : p.Key)];
                    }
                    else
                    {
                        project.CustomSpacing[stem.Replace("{s}", "{0}")] = [];
                    }
                }
                else if (stem.Contains("{c}"))
                {
                    if (GetDictionary(config.OverridenValues[key], out var dict))
                    {
                        project.CustomColors[stem.Replace("{c}", "{0}")] = GetColors(dict, project.Version);
                    }
                    else
                    {
                        project.CustomColors[stem.Replace("{c}", "{0}")] = [];
                    }
                }
                else
                {
                    IEnumerable<TailwindClass> descClasses;
                    var s = stem;

                    if (stem.Contains("{*}"))
                    {
                        s = stem.Replace("-{*}", "");

                        descClasses = project.Classes.Where(c => c.Name.StartsWith(s) && c.HasArbitrary == false && c.UseColors == false && c.UseSpacing == false);
                        classesToRemove.AddRange(descClasses);
                    }
                    else if (stem.Contains('{'))
                    {
                        s = stem.Replace($"-{stem.Split('-').Last()}", "");
                        var values = stem.Split('-').Last().Trim('{', '}').Split('|');

                        bool negate = false;
                        if (values[0].StartsWith("!"))
                        {
                            negate = true;
                            values[0] = values[0].Trim('!');
                        }

                        var classes = values.Select(v => $"{s}-{v}");

                        if (negate)
                        {
                            descClasses = project.Classes.Where(c => c.Name.StartsWith(s) && classes.Contains(c.Name) == false && c.HasArbitrary == false && c.UseColors == false && c.UseSpacing == false);
                        }
                        else
                        {
                            descClasses = project.Classes.Where(c => classes.Contains(c.Name) && c.HasArbitrary == false && c.UseColors == false && c.UseSpacing == false);
                        }
                        classesToRemove.AddRange(descClasses);
                    }
                    else
                    {
                        descClasses = project.Classes.Where(c => c.Name.StartsWith(stem) && c.Name.Replace($"{stem}-", "").Count(ch => ch == '-') == 0 && c.HasArbitrary == false && c.UseColors == false && c.UseSpacing == false);
                        classesToRemove.AddRange(descClasses);
                    }

                    if (GetDictionary(config.OverridenValues[key], out var dict))
                    {
                        // row-span and col-span are actually row and col publicly
                        if (s.EndsWith("-span"))
                        {
                            s = s.Replace("-span", "");
                        }

                        classesToAdd.AddRange(dict.Keys
                            .Where(k => (!project.CustomSpacing.ContainsKey(stem + "-{0}") || !project.CustomSpacing[stem + "-{0}"].Contains(k)) &&
                                              (!project.CustomColors.ContainsKey(stem + "-{0}") || !project.CustomColors[stem + "-{0}"].Contains(k)))
                            .Select(k =>
                            {
                                if (k == "DEFAULT")
                                {
                                    return new TailwindClass()
                                    {
                                        Name = s
                                    };
                                }
                                else if (k.StartsWith('-'))
                                {
                                    return new TailwindClass()
                                    {
                                        Name = $"-{s}-{k.Substring(1)}"
                                    };
                                }
                                else
                                {
                                    return new TailwindClass()
                                    {
                                        Name = $"{s}-{k}"
                                    };
                                }
                            }));
                    }
                }
            }
        }

        project.Classes.RemoveAll(classesToRemove.Contains);
        project.Classes.AddRange(classesToAdd);
    }

    /// <summary>
    /// Reconfigures all classes based on the specified configuration (configures theme.extend.____).
    /// </summary>
    /// <remarks>
    /// Should be called after <see cref="LoadIndividualConfigurationOverride(TailwindConfiguration)"/>.
    /// </remarks>
    /// <param name="config">The configuration object</param>
    private void LoadIndividualConfigurationExtend(ProjectCompletionValues project, TailwindConfiguration config)
    {
        if (config is null)
        {
            return;
        }

        var applicable = project.ConfigurationValueToClassStems.Keys.Where(k => config.ExtendedValues?.ContainsKey(k) == true);

        if (project.Version >= TailwindVersion.V4 && config.ExtendedValues.TryGetValue("screens", out var obj) && obj is Dictionary<string, object> screens)
        {
            foreach (var screen in screens.Keys)
            {
                string[] toInsert = [$"not-{screen}", $"max-{screen}", $"min-{screen}", $"@max-{screen}", $"@min-{screen}"];

                foreach (var insert in toInsert)
                {
                    if (project.Variants.Contains(insert) == false)
                    {
                        project.Variants.Add(insert);
                    }
                }
            }
        }

        var classesToAdd = new List<TailwindClass>();

        foreach (var key in applicable)
        {
            var stems = project.ConfigurationValueToClassStems[key];

            foreach (var stem in stems)
            {
                if (stem.Contains(':'))
                {
                    var s = stem.Trim(':');

                    if (GetDictionary(config.ExtendedValues[key], out var dict))
                    {
                        foreach (var k in dict.Keys)
                        {
                            var insert = k == "DEFAULT" ? s : $"{s}-{k}";

                            if (project.Variants.Contains(insert) == false)
                            {
                                project.Variants.Add(insert);
                            }
                        }
                        ;
                    }
                }
                else if (stem.Contains("{s}"))
                {
                    if (GetDictionary(config.ExtendedValues[key], out var dict))
                    {
                        var newSpacing = project.Spacing.ToHashSet();
                        foreach (var pair in dict)
                        {
                            newSpacing.Add(pair.Key == "DEFAULT" ? "" : pair.Key);
                        }
                        project.CustomSpacing[stem.Replace("{s}", "{0}")] = newSpacing;
                    }
                }
                else if (stem.Contains("{c}"))
                {
                    if (GetDictionary(config.ExtendedValues[key], out var dict))
                    {
                        var newColors = project.Colors.Concat(GetColors(dict, project.Version)).ToHashSet();

                        project.CustomColors[stem.Replace("{c}", "{0}")] = newColors;
                    }
                }
                else
                {
                    var s = stem;
                    IEnumerable<TailwindClass> descClasses;
                    if (stem.Contains("{*}"))
                    {
                        s = stem.Replace("-{*}", "");

                        descClasses = project.Classes.Where(c => c.Name.StartsWith(s) && c.HasArbitrary == false && c.UseColors == false && c.UseSpacing == false);
                    }
                    else if (stem.Contains('{'))
                    {
                        s = stem.Replace($"-{stem.Split('-').Last()}", "");
                        var values = stem.Split('-').Last().Trim('{', '}').Split('|').Select(v => $"{s}-{v}");

                        descClasses = project.Classes.Where(c => values.Contains(c.Name) && c.HasArbitrary == false && c.UseColors == false && c.UseSpacing == false);
                    }
                    else
                    {
                        descClasses = project.Classes.Where(c => c.Name.StartsWith(stem) && c.Name.Replace($"{stem}-", "").Count(ch => ch == '-') == 0 && c.HasArbitrary == false && c.UseColors == false && c.UseSpacing == false);
                    }

                    var insertStem = s;
                    // row-span and col-span are actually row and col publicly
                    if (s.EndsWith("-span"))
                    {
                        insertStem = s.Replace("-span", "");
                    }

                    if (GetDictionary(config.ExtendedValues[key], out var dict))
                    {
                        classesToAdd.AddRange(
                            dict.Keys.Select(k =>
                            {
                                if (k == "DEFAULT")
                                {
                                    return insertStem;
                                }
                                else if (k.StartsWith('-'))
                                {
                                    return $"-{insertStem}-{k.Substring(1)}";
                                }
                                else
                                {
                                    return $"{insertStem}-{k}";
                                }
                            })
                            .Where(k => !string.IsNullOrWhiteSpace(k) && project.Classes.Any(c => c.Name == k) == false)
                            .Select(k =>
                            {
                                return new TailwindClass()
                                {
                                    Name = k
                                };
                            }));
                    }
                }
            }
        }

        // Order by ending number, if applicable, then any text after
        // i.e. inherit, 10, 20, 30, 40, 5, 50 -> 5, 10, 20, 30, 40, 50, inherit
        project.Classes = [.. project.Classes.Concat(classesToAdd)
            .OrderBy(x =>
            {
                if (!x.Name.Contains('-'))
                {
                    return x.Name;
                }

                // Compare the base names (before the last hyphen)
                var xBaseName = x.Name.Substring(0, x.Name.LastIndexOf('-'));

                return xBaseName;
            })
            .ThenBy(x =>
            {
                var last = x.Name.Substring(x.Name.LastIndexOf('-') + 1);
                var xIsNumeric = double.TryParse(last, NumberStyles.Float, CultureInfo.InvariantCulture, out double xNumber);

                if (!xIsNumeric)
                {
                    return 0;
                }

                return xNumber;
            })];
    }

    /// <summary>
    /// Loads IntelliSense for plugins, @custom-variant and @utility
    /// </summary>
    /// <param name="config">The configuration object</param>
    private void LoadPlugins(ProjectCompletionValues project, TailwindConfiguration config)
    {
        if (project.Version >= TailwindVersion.V4)
        {
            // Handle plugin variants

            if (config.PluginDescriptions is not null)
            {
                project.PluginClasses = [];
                var classesToAdd = new List<TailwindClass>();

                foreach (var pair in config.PluginDescriptions)
                {
                    // Case 1: Simple/complex utilities
                    /* 
                    @utility content-auto {
                        content-visibility: auto;
                    }
                    OR 
                    @utility scrollbar-hidden {
                        &::-webkit-scrollbar {
                            display: none;
                        }
                    }
                    */
                    if (!pair.Key.Contains('*'))
                    {
                        project.PluginClasses.Add(pair.Key);
                    }
                    // Case 2 (edge): no --value, but has a wildcard
                    else if (!pair.Value.Contains("--value("))
                    {
                    }
                    // Case 3: --value is present
                    else
                    {
                        // Split based on --value type
                        var valueToDescription = new Dictionary<string, string>();

                        var description = pair.Value.Trim();
                        var splitBySemicolon = CssConfigSplitter.Split(description);

                        var standard = "";

                        foreach (var split in splitBySemicolon)
                        {
                            var attribute = split;

                            // Handle media queries / nested statements
                            if (split.Contains('{'))
                            {
                                var media = split.Substring(0, split.IndexOf('{') + 1);
                                attribute = split[(media.Length + 1)..];

                                if (attribute.Trim().StartsWith('}'))
                                {
                                    // Edge case: empty block
                                    attribute = attribute[(attribute.IndexOf('}') + 1)..];
                                }
                                else
                                {
                                    standard += media;

                                    foreach (var key in valueToDescription.Keys.ToList())
                                    {
                                        valueToDescription[key] += media;
                                    }
                                }
                            }
                            // Handle ending brackets
                            else if (split.Contains('}'))
                            {
                                var bracket = split.Substring(0, split.LastIndexOf('}') + 1);
                                attribute = split.Substring(bracket.Length + 1);
                                standard += bracket;

                                foreach (var key in valueToDescription.Keys.ToList())
                                {
                                    valueToDescription[key] += bracket;
                                }
                            }

                            attribute = attribute?.Trim();

                            if (string.IsNullOrWhiteSpace(attribute))
                            {
                                continue;
                            }
                            // Case 1: No --value
                            else if (!attribute!.Contains("--value("))
                            {
                                standard += attribute;

                                foreach (var key in valueToDescription.Keys.ToList())
                                {
                                    valueToDescription[key] += attribute;
                                }
                            }
                            else
                            {
                                if (!attribute.Contains(':'))
                                {
                                    continue;
                                }

                                var attributeKey = attribute[..attribute.IndexOf(':')];

                                // Handle multiple or single --value
                                // --value(--tab-size-*, integer, [integer])
                                var start = attribute.IndexOf("--value(");

                                var end = attribute.IndexOf(')', start + 1);

                                // Malformed
                                if (start == -1 || end == -1)
                                {
                                    continue;
                                }

                                // integer in --value(integer)
                                var values = attribute.Substring(start + 8, end - start - 8);
                                // --value(integer), full thing
                                var valueMethod = attribute.Substring(start, end - start + 1);

                                var descriptionFormat = $"{attribute.Replace(valueMethod, "{0}")};";

                                foreach (var value in values.Split(','))
                                {
                                    var trimmed = value.Trim();

                                    // Case 1: --theme-variable-*
                                    if (trimmed.StartsWith("--"))
                                    {
                                        // --value does not handle without *
                                        if (!trimmed.EndsWith("-*"))
                                        {
                                            continue;
                                        }

                                        if (valueToDescription.ContainsKey(trimmed) == false)
                                        {
                                            valueToDescription[trimmed] = standard;
                                        }
                                        valueToDescription[trimmed] += descriptionFormat;
                                    }
                                    // Case 2: integer/number
                                    // Case 3: ratio
                                    else if (trimmed == "integer" || trimmed == "number" || trimmed == "ratio")
                                    {
                                        if (valueToDescription.ContainsKey(trimmed) == false)
                                        {
                                            valueToDescription[trimmed] = standard;
                                        }
                                        valueToDescription[trimmed] += descriptionFormat;
                                    }
                                    // Case 4: [...] (arbitrary, such as [integer])
                                    else if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                                    {
                                        if (valueToDescription.ContainsKey(trimmed) == false)
                                        {
                                            valueToDescription[trimmed] = standard;
                                        }
                                        valueToDescription[trimmed] += descriptionFormat;
                                    }
                                }
                            }
                        }

                        foreach (var typeToDescriptionPair in valueToDescription)
                        {
                            var type = typeToDescriptionPair.Key;
                            var desc = typeToDescriptionPair.Value;

                            // Remove --spacing(...)
                            int spacingIndex;
                            var shouldContinue = false;

                            while ((spacingIndex = desc.IndexOf("--spacing(")) != -1)
                            {
                                // Find a balancing pair of parenthesis

                                var start = spacingIndex + "--spacing(".Length;

                                var levels = 1;
                                int end;
                                for (end = start; end < desc.Length; end++)
                                {
                                    var current = desc[end];

                                    if (current == '(')
                                    {
                                        levels++;
                                    }
                                    else if (current == ')')
                                    {
                                        levels--;
                                        if (levels == 0)
                                        {
                                            break;
                                        }
                                    }
                                }

                                if (levels > 0)
                                {
                                    shouldContinue = true;
                                    break;
                                }

                                var parameter = desc.Substring(start, end - start);
                                var total = desc.Substring(spacingIndex, end - spacingIndex + 1);

                                desc = desc.Replace(total, $"calc(var(--spacing) * {parameter})");
                            }

                            if (shouldContinue)
                            {
                                continue;
                            }

                            if (type.StartsWith("--"))
                            {
                                var stem = type.Substring(0, type.Length - 2).Trim();

                                // Special case:
                                // --color-*
                                if (stem == "--color")
                                {
                                    classesToAdd.Add(new()
                                    {
                                        Name = pair.Key.Replace("*", "{0}"),
                                        UseColors = true
                                    });
                                }
                                // Special case 2:
                                // --color-red-* -> this does not do anything
                                else if (!stem.StartsWith("--color-"))
                                {
                                    var variables = project.CssVariables.Where(k => k.Key.StartsWith(stem));

                                    project.PluginClasses.AddRange(variables.Select(
                                        v => pair.Key.Replace("-*", v.Key[stem.Length..])));
                                }
                            }
                        }
                    }
                }

                project.Classes.AddRange(classesToAdd);
            }
        }
        else
        {
            project.PluginClasses = config.PluginClasses;
        }
        project.PluginVariants = config.PluginVariants;
    }

    private static HashSet<string> GetColors(Dictionary<string, object> colors, TailwindVersion version, string prev = "")
    {
        var result = new HashSet<string>();

        foreach (var key in colors.Keys)
        {
            var value = colors[key];

            var actual = prev;

            // when the root key is DEFAULT, it takes no effect on class names
            if (prev == "" || key != "DEFAULT")
            {
                if (actual != "")
                {
                    actual += "-";
                }
                actual += key;
            }

            if (value is string s)
            {
                result.Add(actual);
            }
            else if (value is Dictionary<string, object> colorVariants)
            {
                result.AddRange(GetColors(colorVariants, version, actual));
            }
        }

        return result;
    }
}
