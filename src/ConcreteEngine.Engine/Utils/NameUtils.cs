using System.Text.RegularExpressions;

namespace ConcreteEngine.Engine.Utils;

internal static class NameUtils
{
    private static readonly Regex Pattern = new(@"^[A-Za-z0-9_-]+([:/\.][A-Za-z0-9_-]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    
    public static void ValidateAssetName(string currentName, string name)
    {
        if (currentName == name) throw new ArgumentException("Rename: Identical name", nameof(name));

        if (!Pattern.IsMatch(name))
            throw new FormatException($"Rename: invalid format for '{name}");
        
        var dotCount = name.AsSpan().Count('.');
        if (name.AsSpan().Count('.') > 1) 
            throw new FormatException($"Rename: Max one '.' got {dotCount}");
        
        var startAlphaNumeric = char.IsLetterOrDigit(name[0]);
        var endAlphaNumeric = char.IsLetterOrDigit(name[^1]);
        if (!startAlphaNumeric || !endAlphaNumeric) 
            throw new FormatException($"Rename: Has to start with alpha numeric");


    }
}