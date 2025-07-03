using System.Text.RegularExpressions;

namespace TWSort.Helpers;
public class ClassRegexHelper
{
    // To get the match value, get capture group 'content'
    // https://regex101.com/r/Odcyjx/3
    private static readonly Regex _classRegex = new(@"[cC]lass\s*=\s*(['""])\s*(?<content>(?:\n.|(?!\1).)*)?\s*(\1|$)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex _javaScriptClassRegex = new(@"[cC]lassName\s*=\s*(['""])\s*(?<content>(?:\n.|(?!\1).)*)?\s*(\1|$)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex _razorClassRegex = new(@"[cC]lass(?:es)?\s*=\s*([""'])(?<content>(?:[^""'\\@]|\\.|@(?:[a-zA-Z0-9.]+)?\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)|(?:(?!\1)[^\\]|\\$|\\.)|\([^)]*\))*)(\1|$)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex _razorSplitClassRegex = new(@"(?:(?:@@|@\(""@""\)|[^\s@])+|@[\w.]*\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)|@[\w.]+)+", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex _splitClassRegex = new(@"([^\s]+)", RegexOptions.Compiled);

    // For use on the content capture group of class regexes. No match if there are no single quote pairs.
    // For example, in open ? 'hi @('h')' : '@(Model.Name)', the matches would be 'hi @('h')' and '@(Model.Name)'
    private static readonly Regex _razorQuotePairRegex = new(@"(?<!@\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!)))'(?<content>(?:@\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)|(?:(?!')[^\\]|\\.))*)'", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex _normalQuotePairRegex = new(@"'(?<content>(?:[^'\\]|\\.)*)'", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Gets all class matches in a normal HTML context.
    /// Includes: class="...".
    /// </summary>
    public static IEnumerable<Match> GetClassesNormal(string text, string expandedSearchText)
    {
        return _classRegex.Matches(text).Cast<Match>().SelectMany(match =>
        {
            var classContent = GetClassTextGroup(match);

            if (_normalQuotePairRegex.IsMatch(classContent.Value))
            {
                var lastQuoteMatchIndex = classContent.Index;

                List<Match> pairs = [];

                while (_normalQuotePairRegex.Match(text, lastQuoteMatchIndex, Math.Max(0, classContent.Index + classContent.Length - lastQuoteMatchIndex)) is Match quoteMatch && quoteMatch.Success)
                {
                    lastQuoteMatchIndex = quoteMatch.Index + quoteMatch.Length;
                    pairs.Add(quoteMatch);
                }

                return pairs;
            }

            return [match];
        });
    }

    /// <summary>
    /// Gets all class matches in a razor context.
    /// Includes: class="...".
    /// </summary>
    public static IEnumerable<Match> GetClassesRazor(string text, string expandedSearchText)
    {
        return _razorClassRegex.Matches(text).Cast<Match>().SelectMany(match =>
        {
            var classContent = GetClassTextGroup(match);

            if (_razorQuotePairRegex.IsMatch(classContent.Value))
            {
                var lastQuoteMatchIndex = classContent.Index;

                List<Match> pairs = [];

                while (_razorQuotePairRegex.Match(expandedSearchText, lastQuoteMatchIndex, Math.Max(0, classContent.Index + classContent.Length - lastQuoteMatchIndex)) is Match quoteMatch && quoteMatch.Success)
                {
                    lastQuoteMatchIndex = quoteMatch.Index + quoteMatch.Length;
                    pairs.Add(quoteMatch);
                }

                return pairs;
            }

            return [match];
        });
    }

    /// <summary>
    /// Gets all class matches in a JS context, such as with React.
    /// Includes: className="...".
    /// </summary>
    public static IEnumerable<Match> GetClassesJavaScript(string text, string expandedSearchText)
    {
        return _javaScriptClassRegex.Matches(text).Cast<Match>().SelectMany(match =>
        {
            var classContent = GetClassTextGroup(match);

            if (_normalQuotePairRegex.IsMatch(classContent.Value))
            {
                var lastQuoteMatchIndex = classContent.Index;

                List<Match> pairs = [];

                while (_normalQuotePairRegex.Match(text, lastQuoteMatchIndex, Math.Max(0, classContent.Index + classContent.Length - lastQuoteMatchIndex)) is Match quoteMatch && quoteMatch.Success)
                {
                    lastQuoteMatchIndex = quoteMatch.Index + quoteMatch.Length;
                    pairs.Add(quoteMatch);
                }

                return pairs;
            }

            return [match];
        });
    }

    /// <summary>
    /// Gets all class matches in a razor context.
    /// Includes: class="...". This specific method uses a yield return method.
    /// </summary>
    /// <remarks>
    /// This method does not guarantee that matches are ordered sequentially.
    /// </remarks>
    public static IEnumerable<Match> GetClassesRazorEnumerator(string text)
    {
        var lastMatchIndex = 0;

        while (_razorClassRegex.Match(text, lastMatchIndex) is Match match && match.Success)
        {
            lastMatchIndex = match.Index + match.Length;

            var classText = GetClassTextGroup(match).Value;

            if (_razorQuotePairRegex.IsMatch(classText))
            {
                var lastQuoteMatchIndex = match.Index;

                while (_razorQuotePairRegex.Match(text, lastQuoteMatchIndex, Math.Min(text.Length - lastQuoteMatchIndex, match.Length)) is Match quoteMatch && quoteMatch.Success)
                {
                    lastQuoteMatchIndex = quoteMatch.Index + quoteMatch.Length;
                    yield return quoteMatch;
                }
                continue;
            }

            yield return match;
        }
    }

    /// <summary>
    /// Gets all class matches in a normal context.
    /// Includes: class="...". This specific method uses a yield return method.
    /// </summary>
    /// <remarks>
    /// This method does not guarantee that matches are ordered sequentially.
    /// </remarks>
    public static IEnumerable<Match> GetClassesNormalEnumerator(string text)
    {
        var lastMatchIndex = 0;

        while (_classRegex.Match(text, lastMatchIndex) is Match match && match.Success)
        {
            lastMatchIndex = match.Index + match.Length;

            var classText = GetClassTextGroup(match).Value;

            if (_normalQuotePairRegex.IsMatch(classText))
            {
                var lastQuoteMatchIndex = match.Index;

                while (_normalQuotePairRegex.Match(text, lastQuoteMatchIndex, Math.Min(text.Length - lastQuoteMatchIndex, match.Length)) is Match quoteMatch && quoteMatch.Success)
                {
                    lastQuoteMatchIndex = quoteMatch.Index + quoteMatch.Length;
                    yield return quoteMatch;
                }
                continue;
            }

            yield return match;
        }
    }

    /// <summary>
    /// Gets all class matches in a normal context.
    /// Includes: className="...". This specific method uses a yield return method.
    /// </summary>
    /// <remarks>
    /// This method does not guarantee that matches are ordered sequentially.
    /// </remarks>
    public static IEnumerable<Match> GetClassesJavaScriptEnumerator(string text)
    {
        var lastMatchIndex = 0;

        while (_javaScriptClassRegex.Match(text, lastMatchIndex) is Match match && match.Success)
        {
            lastMatchIndex = match.Index + match.Length;

            var classText = GetClassTextGroup(match).Value;

            if (_normalQuotePairRegex.IsMatch(classText))
            {
                var lastQuoteMatchIndex = match.Index;

                while (_normalQuotePairRegex.Match(text, lastQuoteMatchIndex, Math.Min(text.Length - lastQuoteMatchIndex, match.Length)) is Match quoteMatch && quoteMatch.Success)
                {
                    lastQuoteMatchIndex = quoteMatch.Index + quoteMatch.Length;
                    yield return quoteMatch;
                }
                continue;
            }

            yield return match;
        }
    }

    /// <summary>
    /// Splits the razor class attribute into individual classes; should be called on each Match
    /// from GetClassesRazor
    /// </summary>
    /// <param name="text">An individual razor class context (the ... in class="...")</param>
    public static IEnumerable<Match> SplitRazorClasses(string text)
    {
        var matches = _razorSplitClassRegex.Matches(text).Cast<Match>();
        return matches;
    }

    /// <summary>
    /// Splits the class attribute into individual classes; should be called on each Match
    /// from GetClassesRazor
    /// </summary>
    /// <param name="text">An individual razor class context (the ... in class="...")</param>
    public static IEnumerable<Match> SplitNonRazorClasses(string text)
    {
        var matches = _splitClassRegex.Matches(text).Cast<Match>();
        return matches;
    }

    public static Group GetClassTextGroup(Match match)
    {
        // 'content' capture group matches the class value
        return match.Groups["content"];
    }
}
