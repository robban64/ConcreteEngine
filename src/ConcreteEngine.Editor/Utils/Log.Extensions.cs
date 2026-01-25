using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.UI;

namespace ConcreteEngine.Editor.Utils;

internal static class LogExtensions
{
    extension(LogLevel logLevel)
    {
        public Color4 ToColor()
        {
            return logLevel switch
            {
                LogLevel.Trace => Palette.GrayLight,
                LogLevel.Debug => Palette.BlueLight,
                LogLevel.Info => Palette.GreenBase,
                LogLevel.Warn => Palette.OrangeBase,
                LogLevel.Error => Palette.RedBase,
                LogLevel.Critical => Palette.RedLight,
                _ => Color4.White
            };
        }

        public ReadOnlySpan<byte> ToLogText()
        {
            return logLevel switch
            {
                LogLevel.None => "None"u8,
                LogLevel.Trace => "Trace"u8,
                LogLevel.Debug => "Debug"u8,
                LogLevel.Info => "Info"u8,
                LogLevel.Warn => "Warn"u8,
                LogLevel.Error => "Error"u8,
                LogLevel.Critical => "Critical"u8,
                _ => "Unknown"u8
            };
        }
    }

    extension(LogScope value)
    {
        public ReadOnlySpan<byte> ToLogText()
        {
            return value switch
            {
                LogScope.Unknown => "Unknown"u8,
                LogScope.Engine => "Engine"u8,
                LogScope.Assets => "Asset"u8,
                LogScope.World => "World"u8,
                LogScope.Renderer => "Render"u8,
                LogScope.Gfx => "Graphics"u8,
                LogScope.Backend => "Backend"u8,
                LogScope.Editor => "Editor"u8,
                _ => "Unknown"u8
            };
        }

        public Color4 ToLogColor()
        {
            return value switch
            {
                LogScope.Engine or LogScope.Assets or LogScope.World => Palette.HoverColor,
                LogScope.Renderer => Palette.CyanLight,
                LogScope.Gfx or LogScope.Backend => Palette.BlueLight,
                LogScope.Editor => Palette.GreenLight,
                _ => Palette.TextMuted
            };
        }
    }
}