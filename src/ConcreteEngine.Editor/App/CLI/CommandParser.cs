using System.Globalization;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Command;

namespace ConcreteEngine.Editor.App.CLI;

public static class CommandParser
{
    private static CommandAssetAction ParseAssetAction(string action)
    {
        return action switch
        {
            "reload" => CommandAssetAction.Reload,
            _ => throw new ArgumentException("Unknown action", action)
        };
    }

    private static AssetKind ParseAssetKind(string asset)
    {
        return asset switch
        {
            "shader" => AssetKind.Shader,
            _ => throw new ArgumentException("Unknown asset", asset)
        };
    }


    public static AssetCommandRecord ParseAssetRequest(string action, string arg1, string arg2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(arg1);

        var assetAction = ParseAssetAction(action);
        var assetKind = ParseAssetKind(arg1);
        var asset = ParseUtils.IntArg(arg2);

        return new AssetCommandRecord(assetAction, new AssetId(asset, 0), assetKind);
    }

    private static class ParseUtils
    {
        internal static int IntArg(ReadOnlySpan<char> value)
        {
            if (!int.TryParse(value, CultureInfo.InvariantCulture, out var result))
                throw new FormatException($"Invalid int: '{value.ToString()}'");
            return result;
        }

        internal static float FloatArg(ReadOnlySpan<char> value)
        {
            if (!float.TryParse(value, CultureInfo.InvariantCulture, out var result))
                throw new FormatException($"Invalid float: '{value.ToString()}'");
            return result;
        }

        internal static bool BoolArg(ReadOnlySpan<char> value)
        {
            if (value.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (value.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            throw new FormatException($"Invalid bool: '{value.ToString()}'");
        }
    }
}