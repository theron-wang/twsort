using Spectre.Console;

namespace TWSort.Helpers;
public static class ExceptionLogger
{
    public static void Log(this Exception exception)
    {
        AnsiConsole.WriteException(exception);

        AnsiConsole.MarkupLine($"[yellow]Report the error here: [link]https://github.com/theron-wang/tailwind-class-sorter[/][/]");
    }
}
