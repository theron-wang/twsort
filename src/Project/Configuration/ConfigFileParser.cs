﻿using Spectre.Console;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TailwindCSSIntellisense.Configuration;

namespace TWSort.Project.Configuration;

/// <summary>
/// Parses the Tailwind CSS configuration file
/// </summary>
public static class ConfigFileParser
{
    public static async Task<JsonObject?> GetConfigJsonNodeAsync(string path)
    {
        var scriptLocation = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory)!, "Resources", "parser.js");

        var processInfo = new ProcessStartInfo()
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            FileName = "cmd",
            Arguments = $"/c node \"{scriptLocation}\" \"{Path.GetFileName(path)}\"",
            WorkingDirectory = Path.GetDirectoryName(path)
        };

        var nodePath = await GetNodeModulesFromConfigFilePathAsync(path);

        var globalPath = await GetGlobalPackageLocationAsync();

        var nodePathEnvironmentVariable = processInfo.EnvironmentVariables.ContainsKey("NODE_PATH") ? processInfo.EnvironmentVariables["NODE_PATH"] : null;

        if (!string.IsNullOrWhiteSpace(nodePathEnvironmentVariable))
        {
            nodePathEnvironmentVariable += ";";
        }

        if (nodePath is not null && Directory.Exists(nodePath))
        {
            nodePathEnvironmentVariable += nodePath + ";";
        }

        if (!string.IsNullOrWhiteSpace(globalPath))
        {
            nodePathEnvironmentVariable += globalPath + ";";
        }

        if (!string.IsNullOrWhiteSpace(nodePathEnvironmentVariable))
        {
            nodePathEnvironmentVariable = nodePathEnvironmentVariable.TrimEnd(';');
        }

        processInfo.EnvironmentVariables["NODE_PATH"] = nodePathEnvironmentVariable;

        using var process = Process.Start(processInfo)!;
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var file = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data) == false)
            {
                file.AppendLine(e.Data);
            }
        };
        var hasError = false;
        var error = new StringBuilder();
        process.ErrorDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data) == false && e.Data.Contains("warn") == false)
            {
                hasError = true;
                error.AppendLine(e.Data);
            }
        };
        await process.WaitForExitAsync();

        if (hasError)
        {
            throw new InvalidOperationException("Error occurred while parsing configuration file: " + error.ToString().Trim());
        }

        var fileText = file.ToString().Trim();

        return JsonSerializer.Deserialize<JsonObject>(fileText);
    }

    private static async Task<string> GetGlobalPackageLocationAsync()
    {
        ProcessStartInfo processStartInfo = new()
        {
            FileName = "cmd.exe",
            Arguments = "/C npm root -g",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = Process.Start(processStartInfo)!;

        var output = await process.StandardOutput.ReadToEndAsync();

        await process.WaitForExitAsync();

        return output.Trim();
    }

    /// <summary>
    /// Gets the configuration settings from the Tailwind CSS configuration file.
    /// </summary>
    /// <returns>Returns a <see cref="Task{TailwindConfiguration}" /> of type <see cref="TailwindConfiguration"/> which contains the parsed configuration information</returns>
    public static Task<TailwindConfiguration> GetConfigurationAsync(string path, TailwindVersion version)
    {
        if (Path.GetExtension(path) == ".css")
        {
            return GetCssConfigurationAsync(path, version);
        }
        else
        {
            return GetJavaScriptConfigurationAsync(path);
        }
    }

    private static async Task<TailwindConfiguration> GetCssConfigurationAsync(string path, TailwindVersion version)
    {
        var fullText = "";

        using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            using var reader = new StreamReader(fileStream);

            fullText = await reader.ReadToEndAsync();
        }

        // Remove any text inside {}, since all theme values are in the base theme block
        // We need this because there may be multiple blocks on one line
        var themeTrimmed = new StringBuilder();

        var imports = new List<string>();
        var utilities = new Dictionary<string, string>();
        var variants = new Dictionary<string, string>();
        var content = new List<string>();
        var blocklist = new List<string>();

        var level = 0;
        var inComment = false;

        var directive = "";
        var buildingDirective = false;

        var directiveParameter = "";
        var buildingDirectiveParameter = false;

        var prefix = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            var current = fullText[i];

            if (current == '/' && (!buildingDirectiveParameter || !(directiveParameter.Count(p => p == '\'') == 1 || directiveParameter.Count(p => p == '"') == 1)))
            {
                if (i + 1 < fullText.Length && fullText[i + 1] == '*')
                {
                    inComment = true;
                }
                else if (i > 0 && fullText[i - 1] == '*')
                {
                    inComment = false;
                    continue;
                }
            }

            if (inComment)
            {
                continue;
            }

            if (current == '{' && !buildingDirectiveParameter)
            {
                level++;
            }
            else if (current == '}' && !buildingDirectiveParameter)
            {
                level--;

                if (level == 0)
                {
                    directive = "";
                    directiveParameter = "";
                }
            }

            if (current == '@' && level == 0 && !buildingDirectiveParameter)
            {
                directive = "";
                buildingDirective = true;
                continue;
            }

            if (buildingDirective)
            {
                if (char.IsLetter(current) || current == '-')
                {
                    directive += current;
                }
                else
                {
                    buildingDirective = false;
                    buildingDirectiveParameter = true;
                    directiveParameter = "";
                }
                continue;
            }

            if (buildingDirectiveParameter)
            {
                if (current == ';')
                {
                    directiveParameter = directiveParameter.Trim();
                    buildingDirectiveParameter = false;

                    if (directive == "import")
                    {
                        var firstQuote = directiveParameter.IndexOfAny(['\'', '"']);
                        var secondQuote = directiveParameter.IndexOfAny(['\'', '"'], firstQuote + 1);

                        if (firstQuote == -1 || secondQuote == -1)
                        {
                            continue;
                        }

                        // Handle: @import './_components.css' layer(components);
                        var import = directiveParameter.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim();

                        // Handle these cases:
                        // @import url("https://fonts.googleapis.com/css2?family=Roboto&display=swap");
                        // @import "tailwindcss";
                        if (import != "tailwindcss" && !directiveParameter.StartsWith("url"))
                        {
                            import = PathHelper.GetAbsolutePath(Path.GetDirectoryName(path)!, import);
                            imports.Add($"@import{import}");
                        }
                        // Handle @import "tailwindcss" source(...) and prefix(...)
                        else if (import == "tailwindcss")
                        {
                            if (directiveParameter.IndexOf("source(", secondQuote) is int index && index != -1)
                            {
                                if (directiveParameter.Substring(index + 7).Trim().StartsWith("none"))
                                {
                                    continue;
                                }

                                firstQuote = directiveParameter.IndexOfAny(['\'', '"'], index);
                                secondQuote = directiveParameter.IndexOfAny(['\'', '"'], firstQuote + 1);

                                if (firstQuote == -1 || secondQuote == -1)
                                {
                                    continue;
                                }

                                var source = directiveParameter.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim();

                                source = PathHelper.GetAbsolutePath(Path.GetDirectoryName(path)!, source)!;
                                content.Add(source);
                            }
                            else if (directiveParameter.IndexOf("prefix(", secondQuote) is int prefixIndex && prefixIndex != -1)
                            {
                                var endParen = directiveParameter.IndexOf(')', prefixIndex);

                                if (endParen == -1)
                                {
                                    continue;
                                }

                                prefix = directiveParameter.Substring(prefixIndex + 7, endParen - prefixIndex - 7).Trim();
                            }
                            else
                            {
                                content.Add(Path.GetDirectoryName(path)!);
                            }
                        }
                        continue;
                    }
                    else if (directive == "config")
                    {
                        var import = directiveParameter.Replace("\"", "").Replace("'", "").TrimEnd(';').Trim();

                        import = PathHelper.GetAbsolutePath(Path.GetDirectoryName(path)!, import);
                        imports.Add($"@config{import}");
                        continue;
                    }
                    // Handle short-hand:
                    // @custom-variant pointer-coarse (@media (pointer: coarse))
                    else if (directive == "custom-variant")
                    {
                        var splitAt = directiveParameter.IndexOf(' ');

                        if (splitAt == -1)
                        {
                            continue;
                        }

                        var variantName = directiveParameter.Substring(0, splitAt);
                        var variantValue = directiveParameter.Substring(splitAt + 1).Trim();

                        if (variantValue.StartsWith("(") && variantValue.EndsWith(")"))
                        {
                            variantValue = variantValue.Substring(1, variantValue.Length - 2);
                            variants[variantName] = $"{variantValue} {{ @slot; }}";
                        }

                        continue;
                    }
                    else if (directive == "source")
                    {
                        var not = false;

                        // @source not "../src/components/legacy";
                        // @source not inline("{hover:,focus:,}bg-red-{50,{100..900..100},950}");
                        if (directiveParameter.StartsWith("not"))
                        {
                            not = true;
                            directiveParameter = directiveParameter.Substring(3).Trim();
                        }

                        var firstQuote = directiveParameter.IndexOfAny(['\'', '"']);
                        var secondQuote = directiveParameter.IndexOfAny(['\'', '"'], firstQuote + 1);

                        if (firstQuote == -1 || secondQuote == -1)
                        {
                            continue;
                        }

                        var inQuotes = directiveParameter.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Trim();

                        // @source inline("{hover:,focus:,}bg-red-{50,{100..900..100},950}");
                        if (directiveParameter.StartsWith("inline("))
                        {
                            if (version >= TailwindVersion.V4_1)
                            {
                                blocklist = BraceExpander.Expand(inQuotes);
                            }
                        }
                        else
                        {
                            // @source "../src/components/legacy";
                            var source = PathHelper.GetAbsolutePath(Path.GetDirectoryName(path)!, inQuotes)!;

                            if (not)
                            {
                                content.Add($"!{source}");
                            }
                            else
                            {
                                content.Add(source);
                            }
                        }
                    }

                    directive = "";
                    directiveParameter = "";
                }
                else if (current == '{' && !(directiveParameter.Count(p => p == '\'') == 1 || directiveParameter.Count(p => p == '"') == 1))
                {
                    directiveParameter = directiveParameter.Trim();
                    buildingDirectiveParameter = false;
                    level++;
                }
                else
                {
                    directiveParameter += current;
                }
                continue;
            }

            if (level >= 1)
            {
                if (directive == "theme" && level == 1 && current != '{' && current != '}')
                {
                    themeTrimmed.Append(current);
                }
                else if (directive == "utility")
                {
                    if (!utilities.ContainsKey(directiveParameter))
                    {
                        utilities[directiveParameter] = "";
                    }

                    utilities[directiveParameter] += current.ToString();
                }
                else if (directive == "custom-variant")
                {
                    if (!variants.ContainsKey(directiveParameter))
                    {
                        variants[directiveParameter] = "";
                    }

                    variants[directiveParameter] += current.ToString();
                }
            }
        }

        var themeValuePairs = CssConfigSplitter.Split(themeTrimmed.ToString())
            .Select(s =>
            {
                var split = s.IndexOf(':');
                return new KeyValuePair<string, string>(s.Substring(0, split).Trim(), s.Substring(split + 1).Trim());
            });

        TailwindConfiguration? imported = null;

        foreach (var import in imports)
        {
            // Skips the @import or @config appended at the beginning
            var importPath = import.Substring(7);
            var prev = imported;

            if (import.StartsWith("@import"))
            {
                imported = await GetCssConfigurationAsync(importPath, version);
            }
            else if (import.StartsWith("@config"))
            {
                imported = await GetJavaScriptConfigurationAsync(importPath);
            }
            else
            {
                continue;
            }

            if (prev is not null)
            {
                DictionaryHelpers.MergeDictionaries(imported.OverridenValues, prev.OverridenValues);
                DictionaryHelpers.MergeDictionaries(imported.ExtendedValues, prev.ExtendedValues);
                DictionaryHelpers.MergeDictionaries(imported.ThemeVariables, prev.ThemeVariables);
                imported.PluginClasses.AddRange(prev.PluginClasses);
                imported.PluginVariants.AddRange(prev.PluginVariants);
                imported.Imports.AddRange(prev.Imports);
                imported.Blocklist ??= [.. blocklist];
                imported.Blocklist.AddRange(blocklist);
                DictionaryHelpers.MergeDictionaries(imported.PluginDescriptions, prev.PluginDescriptions);
                DictionaryHelpers.MergeDictionaries(imported.PluginVariantDescriptions, prev.PluginVariantDescriptions);
            }
        }

        if (imported is not null)
        {
            imported.PluginClasses = [.. imported.PluginClasses.Distinct()];
            imported.PluginVariants = [.. imported.PluginVariants.Distinct()];
            imported.Imports = [.. imported.Imports.Distinct()];
            if (imported.Blocklist is not null)
            {
                imported.Blocklist = [.. imported.Blocklist.Distinct()];
            }
        }

        var config = new TailwindConfiguration
        {
            OverridenValues = [],
            ExtendedValues = [],
            ThemeVariables = [],
            PluginClasses = [.. utilities.Keys],
            PluginVariants = [.. variants.Keys],
            PluginDescriptions = utilities,
            // For variants, put everything on the same line so it looks fine in completion tooltip
            PluginVariantDescriptions = variants.ToDictionary(v => v.Key, v =>
                string.Join(" ", v.Value.Split(new char[] { }, StringSplitOptions.RemoveEmptyEntries))),
            Imports = [.. imports.Select(i => i.Replace("@import", "").Replace("@config", ""))],
            ContentPaths = content,
            Blocklist = blocklist,
            Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim()
        };

        foreach (var pair in themeValuePairs)
        {
            var stem = GetConfigurationClassStemFromCssVariable(pair.Key, version);
            var namespaceStem = GetCssVariableNamespace(pair.Key, version);

            if (stem is null || namespaceStem is null)
            {
                config.ThemeVariables[pair.Key.Trim()] = pair.Value.Trim();
                continue;
            }

            var valueKey = pair.Key.Trim().Substring(namespaceStem.Length);

            if (pair.Key.EndsWith("*"))
            {
                config.OverridenValues[stem] = new Dictionary<string, object>();
                config.ExtendedValues[stem] = new Dictionary<string, object>();
                continue;
            }

            Dictionary<string, object> dict;

            if (config.OverridenValues.ContainsKey(stem))
            {
                dict = (Dictionary<string, object>)config.OverridenValues[stem];
            }
            else
            {
                if (!config.ExtendedValues.ContainsKey(stem))
                {
                    config.ExtendedValues[stem] = new Dictionary<string, object>();
                }
                dict = (Dictionary<string, object>)config.ExtendedValues[stem];
            }

            dict[valueKey] = pair.Value.Trim();
        }

        if (imported is not null)
        {
            DictionaryHelpers.MergeDictionaries(config.OverridenValues, imported.OverridenValues);
            DictionaryHelpers.MergeDictionaries(config.ExtendedValues, imported.ExtendedValues);
            DictionaryHelpers.MergeDictionaries(config.ThemeVariables, imported.ThemeVariables);
            config.PluginClasses = [.. config.PluginClasses.Concat(imported.PluginClasses).Distinct()];
            config.PluginVariants = [.. config.PluginVariants.Concat(imported.PluginVariants).Distinct()];
            config.Imports = [.. config.Imports.Concat(imported.Imports).Distinct()];
            if (imported.Blocklist is not null)
            {
                if (config.Blocklist is not null)
                {
                    config.Blocklist = [.. config.Blocklist.Concat(imported.Blocklist).Distinct()];
                }
                else
                {
                    config.Blocklist = imported.Blocklist;
                }
            }
            DictionaryHelpers.MergeDictionaries(config.PluginDescriptions, imported.PluginDescriptions);
            DictionaryHelpers.MergeDictionaries(config.PluginVariantDescriptions, imported.PluginVariantDescriptions);
        }

        return config;
    }

    private static string? GetCssVariableNamespace(string variable, TailwindVersion version)
    {
        if (variable.StartsWith("--color-"))
        {
            return "--color-";
        }
        else if (variable.StartsWith("--font-weight-"))
        {
            return "--font-weight-";
        }
        else if (variable.StartsWith("--font-"))
        {
            return "--font-";
        }
        else if (variable.StartsWith("--text-shadow") && version >= TailwindVersion.V4_1)
        {
            return "--text-shadow";
        }
        else if (variable.StartsWith("--text-"))
        {
            return "--text-";
        }
        else if (variable.StartsWith("--tracking-"))
        {
            return "--tracking-";
        }
        else if (variable.StartsWith("--leading-"))
        {
            return "--leading-";
        }
        else if (variable.StartsWith("--breakpoint-"))
        {
            return "--breakpoint-";
        }
        else if (variable.StartsWith("--container-"))
        {
            // v4
            return "--container-";
        }
        else if (variable.StartsWith("--spacing-"))
        {
            return "--spacing-";
        }
        else if (variable.StartsWith("--radius-"))
        {
            return "--radius-";
        }
        else if (variable.StartsWith("--shadow-"))
        {
            return "--shadow-";
        }
        else if (variable.StartsWith("--inset-shadow-"))
        {
            // v4
            return "--inset-shadow-";
        }
        else if (variable.StartsWith("--drop-shadow-"))
        {
            return "--drop-shadow-";
        }
        else if (variable.StartsWith("--blur-"))
        {
            return "--blur-";
        }
        else if (variable.StartsWith("--perspective-"))
        {
            // v4
            return "--perspective-";
        }
        else if (variable.StartsWith("--aspect-"))
        {
            return "--aspect-";
        }
        else if (variable.StartsWith("--ease-"))
        {
            return "--ease-";
        }
        else if (variable.StartsWith("--animate-"))
        {
            return "--animate-";
        }

        return null;
    }

    private static string? GetConfigurationClassStemFromCssVariable(string variable, TailwindVersion version)
    {
        if (variable.StartsWith("--color-"))
        {
            return "colors";
        }
        else if (variable.StartsWith("--font-weight-"))
        {
            return "fontWeight";
        }
        else if (variable.StartsWith("--font-"))
        {
            return "fontFamily";
        }
        else if (variable.StartsWith("--text-shadow") && version >= TailwindVersion.V4_1)
        {
            // 4.1+
            return "v4_1-text-shadow";
        }
        else if (variable.StartsWith("--text-"))
        {
            return "fontSize";
        }
        else if (variable.StartsWith("--tracking-"))
        {
            return "letterSpacing";
        }
        else if (variable.StartsWith("--leading-"))
        {
            return "lineHeight";
        }
        else if (variable.StartsWith("--breakpoint-"))
        {
            return "screens";
        }
        else if (variable.StartsWith("--container-"))
        {
            // v4
            return "v4-container";
        }
        else if (variable.StartsWith("--spacing-"))
        {
            return "spacing";
        }
        else if (variable.StartsWith("--radius-"))
        {
            return "borderRadius";
        }
        else if (variable.StartsWith("--shadow-"))
        {
            return "boxShadow";
        }
        else if (variable.StartsWith("--inset-shadow-"))
        {
            // v4
            return "v4-insetShadow";
        }
        else if (variable.StartsWith("--drop-shadow-"))
        {
            return "dropShadow";
        }
        else if (variable.StartsWith("--blur-"))
        {
            return "blur";
        }
        else if (variable.StartsWith("--perspective-"))
        {
            // v4
            return "v4-perspective";
        }
        else if (variable.StartsWith("--aspect-"))
        {
            return "aspectRatio";
        }
        else if (variable.StartsWith("--ease-"))
        {
            return "transitionTimingFunction";
        }
        else if (variable.StartsWith("--animate-"))
        {
            return "animation";
        }

        return null;
    }

    private static async Task<TailwindConfiguration> GetJavaScriptConfigurationAsync(string path)
    {
        var obj = await GetConfigJsonNodeAsync(path);

        if (obj is null)
        {
            return new TailwindConfiguration
            {
                OverridenValues = [],
                ExtendedValues = []
            };
        }

        if (obj.Count == 1 && obj.ContainsKey("default"))
        {
            obj = obj["default"]!.AsObject();
        }

        var theme = obj["theme"];

        var plugins = GetTotalValue(obj["plugins"]) ?? [];

        var config = new TailwindConfiguration
        {
            OverridenValues = theme is null ? [] : GetTotalValue(theme, "extend") ?? [],
            ExtendedValues = theme is null ? [] : GetTotalValue(theme["extend"]) ?? [],
            Prefix = obj["prefix"]?.ToString(),
            ContentPaths = obj["content"] is null ? [] : obj["content"].Deserialize<List<string>>()!
        };

        try
        {
            config.PluginVariants = plugins.ContainsKey("variants") ? (List<string>)plugins["variants"] : [];

            config.PluginClasses = [];
            if (plugins.TryGetValue("classes", out object? value))
            {
                var classes = (List<string>)value;

                foreach (var item in classes)
                {
                    if (item.Contains("@media") || item.Contains("@font-face") || item.Contains("@keyframes") || item.Contains("@supports"))
                    {
                        continue;
                    }
                    var commaSplitClasses = item.Split([",", " .", "."], StringSplitOptions.RemoveEmptyEntries);

                    foreach (var className in commaSplitClasses)
                    {
                        string toAdd = className.Trim();
                        if (toAdd.StartsWith('[') && toAdd.EndsWith(']'))
                        {
                            continue;
                        }
                        if (toAdd.Contains('[') && toAdd.IndexOf('[') != toAdd.IndexOf("-[") + 1)
                        {
                            toAdd = toAdd[..toAdd.IndexOf('[')];
                        }
                        if (toAdd.Contains(':'))
                        {
                            toAdd = toAdd[..toAdd.IndexOf(':')];
                        }
                        if (toAdd.Contains(' '))
                        {
                            toAdd = toAdd[..toAdd.IndexOf(' ')];
                        }

                        toAdd = toAdd.TrimEnd(')');

                        if (config.PluginClasses.Contains(toAdd) == false && string.IsNullOrEmpty(toAdd) == false)
                        {
                            config.PluginClasses.Add(toAdd);
                        }
                    }
                }
            }

            if (obj["plugins"]?["descriptions"] is not null)
            {
                config.PluginDescriptions = obj["plugins"]!["descriptions"]!.Deserialize<Dictionary<string, string>>()!;
            }
        }
        catch (Exception ex)
        {
            ex.Log();
        }

        try
        {
            if (obj["blocklist"] is not null)
            {
                config.Blocklist = obj["blocklist"].Deserialize<List<string>>()!;
            }
        }
        catch (Exception ex)
        {
            ex.Log();
        }


        try
        {
            if (obj["corePlugins"] is not null)
            {
                if (GetValueKind(obj["corePlugins"]) == JsonValueKind.Array)
                {
                    config.EnabledCorePlugins = obj["corePlugins"].Deserialize<List<string>>()!;
                }
                else
                {
                    config.DisabledCorePlugins = [.. obj["corePlugins"].Deserialize<Dictionary<string, bool>>()!
                        .Where(kvp => !kvp.Value)
                        .Select(kvp => kvp.Key)];
                }
            }
        }
        catch (Exception ex)
        {
            ex.Log();
        }

        return config;
    }

    private static async Task<string?> GetNodeModulesFromConfigFilePathAsync(string configPath)
    {
        var processInfo = new ProcessStartInfo()
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            FileName = "cmd",
            Arguments = "/C npm root",
            WorkingDirectory = Path.GetDirectoryName(configPath)!
        };

        string output;

        using (var process = Process.Start(processInfo))
        {
            output = await process!.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();
        }

        output = output.Trim();

        if (Directory.Exists(output))
        {
            return output;
        }

        return null;
    }

    private static Dictionary<string, object>? GetTotalValue(JsonNode? node, string? ignoreKey = null)
    {
        if (node is null)
        {
            return null;
        }

        var result = new Dictionary<string, object>();

        if (GetValueKind(node) == JsonValueKind.Object)
        {
            foreach (var key in GetKeys(node))
            {
                if (key == ignoreKey)
                {
                    continue;
                }

                var value = node[key];

                var valueKind = GetValueKind(value);

                if (valueKind == JsonValueKind.Null || value is null)
                {
                    continue;
                }

                if (valueKind == JsonValueKind.Object)
                {
                    result[key] = GetTotalValue(value)!;
                }
                else if (valueKind == JsonValueKind.Array)
                {
                    result[key] = value.AsArray().Select(n => n?.ToString()).ToList();
                }
                else
                {
                    result[key] = value.ToString().Trim();
                }
            }
        }

        return result;
    }

    private static ICollection<string> GetKeys(JsonNode obj)
    {
        return ((IDictionary<string, JsonNode>)obj).Keys;
    }

    private static JsonValueKind GetValueKind(JsonNode? node)
    {
        if (node is JsonObject)
        {
            return JsonValueKind.Object;
        }
        else if (node is JsonArray)
        {
            return JsonValueKind.Array;
        }
        else if (node is null)
        {
            return JsonValueKind.Null;
        }

        var value = node.GetValue<JsonElement>();

        return value.ValueKind;
    }
}
