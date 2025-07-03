﻿namespace TWSort.Sorters;

public class CssSorter : Sorter
{
    public override string[] Handled { get; } = [".css", ".tcss"];

    protected override IEnumerable<string> GetSegments(string filePath, string content)
    {
        int indexOfApply = content.IndexOf("@apply");

        int lastIndex = 0;

        while (indexOfApply != -1)
        {
            // Verify that we are in the right context
            // Check to see if it is whitespace between here and { or ;, whichever comes first
            var context = content.LastIndexOfAny([';', '{'], indexOfApply) + 1;

            if (context == -1 || !string.IsNullOrWhiteSpace(content.Substring(context, indexOfApply - context)))
            {
                indexOfApply = content.IndexOf("@apply", indexOfApply + 1);
                continue;
            }

            yield return content.Substring(lastIndex, indexOfApply - lastIndex);

            lastIndex = content.IndexOfAny([';', '}'], indexOfApply);

            if (lastIndex == -1)
            {
                yield return content.Substring(indexOfApply);
                yield break;
            }

            // return @apply
            yield return "@apply ";
            yield return SortSegment(content.Substring(indexOfApply + 6, lastIndex - (indexOfApply + 6)), filePath);
            indexOfApply = content.IndexOf("@apply", indexOfApply + 1);
        }

        yield return content.Substring(lastIndex);
    }
}
