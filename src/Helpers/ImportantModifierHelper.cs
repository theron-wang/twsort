﻿namespace TWSort.Helpers;
public class ImportantModifierHelper
{
    public static bool IsImportantModifier(string classText)
    {
        return classText.StartsWith('!') && !(classText.Length >= 2 && classText[1] == '!') || classText.EndsWith('!') && !(classText.Length >= 2 && classText[^2] == '!');
    }
}
