using Spectre.Console;
using System.CommandLine;
using System.Diagnostics;
using TWSort;
using TWSort.Files;
using TWSort.Project;

var inputArgument = new Argument<string>("input")
{
    Description = "File or directory to process"
};

var shallowOption = new Option<bool>("--shallow")
{
    Description = "Sort only the classes in the specified directory without recursing into subdirectories"
};

var extensionsOption = new Option<string>("--extensions")
{
    Description = "Comma-separated file extensions to process",
    DefaultValueFactory = parseResult => "css,html,aspx,ascx,jsx,tsx,razor,cshtml"
};

var tailwindVersionOption = new Option<string>("--tailwind-version")
{
    Description = "Specify Tailwind major version (e.g. 3, 4, 4.1). Use if auto-detection fails."
};

var configurationFileOption = new Option<string[]>("--config")
{
    Description = "Paths to all Tailwind CSS configuration files. In single file sort mode, this option must be provided. In directory sort mode, this is optional; if not provided, the tool will attempt to find configuration files automatically.",
    AllowMultipleArgumentsPerToken = true
};

var verboseOption = new Option<bool>("--verbose")
{
    Description = "Show full logs"
};

var rootCommand = new RootCommand("Tailwind Class Sorter – Sorts Tailwind CSS classes for consistency and cleanliness")
{
    inputArgument,
    shallowOption,
    extensionsOption,
    tailwindVersionOption,
    configurationFileOption,
    verboseOption
};

rootCommand.SetAction(async (parseResult, token) =>
{
    var input = PathHelper.GetAbsolutePath(Environment.CurrentDirectory, parseResult.GetValue(inputArgument));
    var recursive = !parseResult.GetValue(shallowOption);
    var extensions = parseResult.GetValue(extensionsOption) ?? "";
    var tailwindVersion = parseResult.GetValue(tailwindVersionOption);
    var configurationFiles = parseResult.GetValue(configurationFileOption);
    var verbose = parseResult.GetValue(verboseOption);

    if (string.IsNullOrWhiteSpace(input))
    {
        AnsiConsole.MarkupLine("[red]Error:[/] No input file or directory provided.");
        return 1;
    }

    var extList = extensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            .Select(e => e.StartsWith('.') ? e : "." + e)
                            .ToHashSet();

    var inputPath = Path.GetFullPath(input);

    List<string> filesToProcess = [];

    if (File.Exists(inputPath))
    {
        if (extList.Contains(Path.GetExtension(inputPath)))
            filesToProcess.Add(inputPath);

        if (configurationFiles is null || configurationFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] A configuration file must be provided using --config in single file sort mode.");
            return 1;
        }
    }
    else if (Directory.Exists(inputPath))
    {
        var allFiles = FileFinder.FindFilesInDirectory(inputPath, recursive, extList);
        filesToProcess = [.. allFiles.Where(file => extList.Contains(Path.GetExtension(file)))];
    }
    else
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] Path does not exist: {inputPath}");
        return 1;
    }

    if (filesToProcess.Count == 0)
    {
        AnsiConsole.MarkupLine($"[yellow]No matching files found with extensions: {string.Join(", ", extList)}[/]");
        return 1;
    }

    try
    {
        if (!string.IsNullOrWhiteSpace(tailwindVersion))
        {
            if (tailwindVersion.StartsWith("4.0"))
            {
                DirectoryVersionFinder.SetTailwindVersion(TailwindVersion.V4);
            }
            else if (tailwindVersion.StartsWith('4'))
            {
                DirectoryVersionFinder.SetTailwindVersion(TailwindVersion.V4_1);
            }
            else if (tailwindVersion.StartsWith('3'))
            {
                DirectoryVersionFinder.SetTailwindVersion(TailwindVersion.V3);
            }
        }

        var files = configurationFiles is not null && configurationFiles.Length > 0
            ? configurationFiles
                .Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => PathHelper.GetAbsolutePath(Environment.CurrentDirectory, f)!)
            : FileFinder.TryFindConfigurationFiles(input, recursive).ToBlockingEnumerable(token);

        var usingProvided = false;

        if (configurationFiles is not null && configurationFiles.Length > 0)
        {
            AnsiConsole.MarkupLine("[cyan]Using provided configuration files:[/]");
            usingProvided = true;
        }
        else
        {
            AnsiConsole.MarkupLine("[cyan]Automatically found these Tailwind CSS configuration files:[/]");
        }

        AnsiConsole.WriteLine();

        foreach (var file in files)
        {
            if (usingProvided && !File.Exists(file))
            {
                AnsiConsole.MarkupLine($"* [red underline]{file} (does not exist)[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"* [link]{file}[/]");
            }
        }

        await ProjectConfigurationManager.Instance.Initialize(files.Where(File.Exists), verbose);
    }
    catch (Exception ex)
    {
        ex.Log();
    }

    AnsiConsole.WriteLine();

    var stopwatch = new Stopwatch();

    if (File.Exists(inputPath))
    {
        AnsiConsole.MarkupLine($"[yellow]Sorting:[/] [link]{inputPath}[/]");

        stopwatch.Start();
        await ClassSorter.SortAsync(inputPath);
    }
    else
    {
        AnsiConsole.MarkupLine($"[cyan]Found {filesToProcess.Count} file(s)[/]");

        stopwatch.Start();
        await AnsiConsole.Progress()
           .StartAsync(async ctx =>
           {
               var task = ctx.AddTask($"Sorting files (0/{filesToProcess.Count} done)");
               string? currentFile = null;

               for (int i = 0; i < filesToProcess.Count && !task.IsFinished; i++)
               {
                   currentFile = filesToProcess[i];

                   if (verbose)
                   {
                       AnsiConsole.MarkupLine($"[yellow]Sorting:[/] {PathHelper.GetRelativePath(currentFile, Environment.CurrentDirectory)}");
                   }

                   try
                   {
                       await ClassSorter.SortAsync(currentFile);
                   }
                   catch (Exception ex)
                   {
                       if (string.IsNullOrWhiteSpace(currentFile))
                       {
                           AnsiConsole.WriteException(ex);
                       }
                       else
                       {
                           AnsiConsole.MarkupLine($"[red]Error:[/] The following error occurred while sorting [link]{currentFile}[/]");
                           AnsiConsole.WriteException(ex);
                       }

                       AnsiConsole.MarkupLine($"[yellow]Report the error here: [link]https://github.com/theron-wang/tailwind-class-sorter[/][/]");

                       AnsiConsole.WriteLine();
                   }

                   task.Increment(100.0 / filesToProcess.Count);
                   task.Description = $"Sorting files ({i + 1}/{filesToProcess.Count} done)";
               }

               task.Value = task.MaxValue;
           });
    }

    stopwatch.Stop();

    if (stopwatch.Elapsed.TotalSeconds > 1)
    {
        AnsiConsole.MarkupLine($"[green]Done in {stopwatch.Elapsed.TotalSeconds:0.00}s.[/]");
    }
    else
    {
        AnsiConsole.MarkupLine($"[green]Done in {stopwatch.ElapsedMilliseconds}ms.[/]");
    }

    return 0;
});

return rootCommand.Parse(args).Invoke();