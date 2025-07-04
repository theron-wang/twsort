﻿namespace TailwindCSSIntellisense.Configuration;

public class TailwindConfiguration
{
    /// <summary>
    /// Glob format for v3, base folders for v4. In v4, the folders may start with !. If ! is the first character, that means that
    /// folder is excluded.
    /// </summary>
    public List<string> ContentPaths { get; set; } = [];

    /// <summary>
    /// Corresponds to theme.____. This value will NEVER be null.
    /// </summary>
    /// <remarks>
    /// The key is the class being overriden, such as color. <br></br>
    /// The value is a <see cref="Dictionary{TKey, TValue}"/>, which holds the specific key/value pairs of the overriden values.
    /// </remarks>
    public Dictionary<string, object> OverridenValues { get; set; } = [];

    /// <summary>
    /// Corresponds to theme.extend.____. This value will NEVER be null.
    /// </summary>
    /// <remarks>
    /// The key is the class being extended, such as color. <br></br>
    /// The value is a <see cref="Dictionary{TKey, TValue}"/>, which holds the specific key/value pairs of the extended values.
    /// </remarks>
    public Dictionary<string, object> ExtendedValues { get; set; } = [];

    /// <summary>
    /// V4 only
    /// </summary>
    public Dictionary<string, string> ThemeVariables { get; set; } = [];

    public string? Prefix { get; set; }

    public List<string> PluginClasses { get; set; } = [];

    public List<string> PluginVariants { get; set; } = [];

    public Dictionary<string, string> PluginDescriptions { get; set; } = [];

    public Dictionary<string, string> PluginVariantDescriptions { get; set; } = [];

    public List<string>? Blocklist { get; set; }

    public List<string>? EnabledCorePlugins { get; set; }

    public List<string>? DisabledCorePlugins { get; set; }

    public List<string> Imports { get; set; } = [];
}
