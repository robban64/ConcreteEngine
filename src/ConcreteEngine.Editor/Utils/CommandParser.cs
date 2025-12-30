using System.Globalization;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Metadata.Command;

namespace ConcreteEngine.Editor.Utils;

public static class CommandParser
{
    private static CommandAssetAction ParseAssetAction(string action)
    {
        return action switch
        {
            "reload" => CommandAssetAction.Reload,
            _ => throw new ArgumentException("Unknown action", nameof(action))
        };
    }

    private static AssetKind ParseAssetKind(string asset)
    {
        return asset switch
        {
            "shader" => AssetKind.Shader,
            _ => throw new ArgumentException("Unknown asset", nameof(asset))
        };
    }


    public static FboCommandRecord ParseShadowRequest(string action, string? arg1, string? arg2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(arg1);

        if (action != "set") throw new ArgumentException("Unknown action", nameof(action));

        var size = ParseUtils.GetShadowSize(ParseUtils.IntArg(arg1));

        if (size <= 0)
            throw new ArgumentException("Supported are 1,2,4,8 (1024, 2048, 4096, 8192)");

        return new FboCommandRecord(CommandFboAction.ShadowSize, new Size2D(size));
    }

    public static AssetCommandRecord ParseAssetRequest(string action, string? arg1, string? arg2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(arg1);
        ArgumentException.ThrowIfNullOrWhiteSpace(arg2);

        var assetAction = ParseAssetAction(action);
        var assetKind = ParseAssetKind(arg1);

        return new AssetCommandRecord(assetAction, assetKind, arg2);
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

        internal static int GetShadowSize(int size)
        {
            size = size switch
            {
                1 => 1024,
                2 => 2048,
                4 => 4096,
                8 => 8192,
                _ => size
            };

            if (size is 1024 or 2048 or 4096 or 8192) return size;

            return -1;
        }
    }
}