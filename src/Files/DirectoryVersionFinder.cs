using System.Diagnostics;

namespace TWSort.Files;

public static class DirectoryVersionFinder
{
    private static TailwindVersion? _specifiedVersion;

    private static readonly Dictionary<string, TailwindVersion> _cache = [];

    public static void SetTailwindVersion(TailwindVersion version)
    {
        _specifiedVersion = version;
    }

    /// <summary>
    /// Gets the tailwind version for the directory of the given file. Caches the result.
    /// </summary>
    public static async Task<TailwindVersion> GetTailwindVersionAsync(string file)
    {
        if (_specifiedVersion is not null)
        {
            return _specifiedVersion.Value;
        }

        var directory = Path.GetDirectoryName(file)!.ToLower();

        if (_cache.TryGetValue(directory, out var version))
        {
            return version;
        }

        var processInfo = new ProcessStartInfo()
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            FileName = "cmd",
            Arguments = "/C npm list tailwindcss --depth=0",
            WorkingDirectory = directory
        };

        string output;

        using (var process = Process.Start(processInfo))
        {
            output = await process!.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();
        }

        // If not found locally, default to global
        if (string.IsNullOrWhiteSpace(output) || !output.Contains("tailwindcss"))
        {
            processInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                FileName = "cmd",
                Arguments = "/C npm list tailwindcss --depth=0 -g",
                WorkingDirectory = directory
            };

            using var process = Process.Start(processInfo);

            output = await process!.StandardOutput.ReadToEndAsync();

            await process.WaitForExitAsync();
        }

        // Sample output: `-- tailwindcss@4.0.0
        if (!string.IsNullOrWhiteSpace(output) && output.Contains("@4"))
        {
            if (output.Contains("@4.1"))
            {
                _cache[directory] = TailwindVersion.V4_1;
            }
            else
            {
                _cache[directory] = TailwindVersion.V4;
            }

            return _cache[directory];
        }

        // Fallback: 3
        _cache[directory] = TailwindVersion.V3;
        return TailwindVersion.V3;
    }
}