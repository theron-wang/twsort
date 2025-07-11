﻿using TWSort.Helpers;

namespace TWSort.Sorters;
public class HtmlSorter : Sorter
{
    public override string[] Handled { get; } = [".html", ".aspx", ".ascx"];

    protected override IEnumerable<string> GetSegments(string filePath, string content)
    {
        int lastIndex = 0;
        int indexOfClass;

        foreach (var match in ClassRegexHelper.GetClassesNormalEnumerator(content))
        {
            indexOfClass = match.Index;

            // If we've already crossed over this part, don't sort again
            if (indexOfClass < lastIndex)
            {
                continue;
            }

            // Verify that we are in an HTML tag
            var closeAngleBracket = content.LastIndexOf('>', indexOfClass);
            var openAngleBracket = content.LastIndexOf('<', indexOfClass);

            if (openAngleBracket == -1 || closeAngleBracket > openAngleBracket)
            {
                continue;
            }

            yield return content.Substring(lastIndex, indexOfClass - lastIndex);

            lastIndex = match.Index + match.Length;

            if (lastIndex >= content.Length)
            {
                yield return content.Substring(indexOfClass);
                yield break;
            }

            // returns the text up to the content capture group (such as class=" or class=')
            var total = match.Value;
            var classContent = ClassRegexHelper.GetClassTextGroup(match).Value;
            yield return total.Substring(0, total.IndexOf(classContent));

            yield return SortSegment(classContent, filePath);
            yield return total.Substring(total.IndexOf(classContent) + classContent.Length);
        }

        yield return content.Substring(lastIndex);
    }
}
