namespace TWSort.Files;

public static class FileFinder
{
    private static readonly string[] _javascriptExtensions = [".js", ".cjs", ".mjs", ".ts", ".cts", ".mts"];
    private static readonly string[] _javascriptConfigNames = [.. _javascriptExtensions.Select(e => $"tailwind.config{e}")];

    public static IEnumerable<string> FindFilesInDirectory(string baseDirectory, bool isRecursive, IEnumerable<string> extensions)
    {
        return Directory
             .EnumerateFiles(baseDirectory, "*.*", isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
             .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()) &&
                            !file.Split(Path.DirectorySeparatorChar).Contains("node_modules"));
    }

    /// <summary>
    /// Searches through the solution to find a Tailwind CSS configuration file.
    /// </summary>
    /// <returns>A <see cref="Task"/> of type <see cref="string" />, which represents the absolute path to an existing configuration file, or null if one cannot be found</returns>
    internal static async IAsyncEnumerable<string> TryFindConfigurationFiles(string baseDirectory, bool isRecursive)
    {
        var allFiles = FindFilesInDirectory(baseDirectory, isRecursive, _javascriptExtensions.Concat([".css"]));

        var cssFiles = allFiles.Where(p => Path.GetExtension(p) == ".css");

        foreach (var file in cssFiles)
        {
            // Priority: check for @import "tailwindcss";
            if (await DoesFileContainAsync(file, "@import", "tailwindcss"))
            {
                yield return file;
            }

            // Next: search all css files and scrape for @config
            if (await DoesFileContainAsync(file, "@config"))
            {
                var configFile = await ExtractConfigJsPathAsync(file);

                if (File.Exists(configFile))
                {
                    yield return configFile;
                }
            }
        }

        // Finally, check for tailwind.config.{js,ts,etc.}
        var jsFiles = allFiles.Where(p => _javascriptExtensions.Contains(Path.GetExtension(p)));

        foreach (var file in jsFiles)
        {
            if (_javascriptConfigNames.Contains(Path.GetFileName(file).ToLower()))
            {
                yield return file;
            }
        }
    }

    private static async Task<bool> DoesFileContainAsync(string filePath, string text)
    {
        using var fs = File.OpenRead(filePath);
        using var reader = new StreamReader(fs);

        // Read up to line 15
        for (int i = 0; i < 15; i++)
        {
            var line = await reader.ReadLineAsync();

            if (line is null)
            {
                break;
            }

            if (line.Contains(text))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a file contains text and text1 on the same line. For example, use @import and tailwindcss to match @import "tailwindcss";
    /// </summary>
    private static async Task<bool> DoesFileContainAsync(string filePath, string text, string text1)
    {
        using var fs = File.OpenRead(filePath);
        using var reader = new StreamReader(fs);

        // Read up to line 15
        for (int i = 0; i < 15; i++)
        {
            var line = await reader.ReadLineAsync();

            if (line is null)
            {
                break;
            }

            if (line.Contains(text) && line.Contains(text1))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<string?> ExtractConfigJsPathAsync(string filePath)
    {
        string? configLine = null;
        // Read up to line 15
        var lines = 0;
        using (var fs = File.OpenRead(filePath))
        {
            using var reader = new StreamReader(fs);
            var line = await reader.ReadLineAsync();
            lines++;

            if (line is not null && line.Contains("@config"))
            {
                configLine = line.Trim();
                goto End;
            }

            if (lines > 15)
            {
                goto End;
            }
        }

    End:

        if (configLine == null)
        {
            return null;
        }
        var indexOfConfig = configLine.IndexOf("@config");
        var indexOfSemicolon = configLine.IndexOf(';', indexOfConfig);

        string scanText;
        if (indexOfSemicolon == -1)
        {
            scanText = configLine[indexOfConfig..];
        }
        else
        {
            scanText = configLine[indexOfConfig..indexOfSemicolon];
        }

        try
        {
            var relPath = scanText.Split(' ')[1].Trim('\'').Trim('\"');

            // @config provides a relative path to configuration file
            // To find the path of the config file, we must take the relative path in terms
            // of the absolute path of the css file
            return Uri.UnescapeDataString(new Uri(new Uri(filePath), relPath).AbsolutePath);
        }
        catch
        {
            // @config syntax is invalid, cannot parse path
            return null;
        }
    }
}