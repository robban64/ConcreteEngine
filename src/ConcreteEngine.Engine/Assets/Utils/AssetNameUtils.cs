using System.Text.RegularExpressions;

namespace ConcreteEngine.Engine.Assets.Utils;

internal static class AssetNameUtils
{
    private static readonly Regex Pattern = new("^[A-Za-z0-9_-]+(?:(?:::?)[A-Za-z0-9_-]+|[/.][A-Za-z0-9_-]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string IncrementName(string name, Type type, Func<string, Type, bool> validate, int maxRetries = 100)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxRetries);

        if (validate(name,type)) return name;

        for (int i = 0; i < maxRetries; i++)
        {
            var newName = $"{name}_{i}";
            if (validate(newName,type)) return newName;
        }

        throw new InvalidOperationException($"Asset '{name}' increment name. Max retries reached");
    }

    public static bool IsValidName(string name, out string errorMessage)
    {
        try
        {
            ValidateAssetName(name);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or FormatException)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public static void ValidateAssetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(name.Length, 3, nameof(name));
        ArgumentOutOfRangeException.ThrowIfLessThan(GetLetterCount(name), 3, nameof(name));

        if (!Pattern.IsMatch(name))
            throw new FormatException($"Rename: invalid format for '{name}");

        var dotCount = name.AsSpan().Count('.');
        if (name.AsSpan().Count('.') > 1)
            throw new FormatException($"Rename: Max one '.' got {dotCount}");

        var startAlphaNumeric = char.IsLetterOrDigit(name[0]);
        var endAlphaNumeric = char.IsLetterOrDigit(name[^1]);
        if (!startAlphaNumeric || !endAlphaNumeric)
            throw new FormatException($"Rename: Has to start and end with alpha numeric");
    }

    private static int GetLetterCount(ReadOnlySpan<char> span)
    {
        int count = 0;
        foreach (var c in span)
        {
            if (char.IsLetter(c)) count++;
        }

        return count;
    }
}