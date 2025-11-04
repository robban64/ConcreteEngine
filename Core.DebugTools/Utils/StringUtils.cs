namespace Core.DebugTools.Utils;

internal static class StringUtils
{
    
    public static string BoolToYesNo(bool value, bool longVersion = false)
    {
        if(longVersion) return value ? "Yes" : "No";
        return value ? "Y" : "N";
    }
}