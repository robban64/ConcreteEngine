using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.UI;

namespace ConcreteEngine.Editor.Utils;

internal static class LogExtensions
{
    extension(LogLevel logLevel)
    {
        public string ToLogText()
        {
            return logLevel switch
            {
                LogLevel.None => "None",
                LogLevel.Trace => "Trace",
                LogLevel.Debug => "Debug",
                LogLevel.Info => "Info",
                LogLevel.Warn => "Warn",
                LogLevel.Error => "Error",
                LogLevel.Critical => "Critical",
                _ => "Unknown"
            };
        }
    }

    extension(LogScope value)
    {
        public string ToLogText()
        {
            return value switch
            {
                LogScope.Command => "Command",
                LogScope.Engine => "Engine",
                LogScope.Assets => "Asset",
                LogScope.World => "World",
                LogScope.Renderer => "Render",
                LogScope.Gfx => "Graphics",
                LogScope.Backend => "Backend",
                LogScope.Editor => "Editor",
                _ => "Unknown"
            };
        }

    }
}