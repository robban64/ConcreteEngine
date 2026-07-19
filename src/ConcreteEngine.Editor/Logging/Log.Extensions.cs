using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Editor.Logging;

internal static class LogExtensions
{
    extension(LogLevel logLevel)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ToLogText()
        {
            return logLevel switch
            {
                LogLevel.None => "[None]"u8,
                LogLevel.Trace => "[Trace]"u8,
                LogLevel.Debug => "[Debug]"u8,
                LogLevel.Info => "[Info] "u8,
                LogLevel.Warn => "[Warn] "u8,
                LogLevel.Error => "[Error]"u8,
                LogLevel.Critical => "[Critc]"u8,
                _ => "[Unknown]"u8
            };
        }
    }

    extension(LogScope value)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ToLogText()
        {
            return value switch
            {
                LogScope.Command => "[Command]"u8,
                LogScope.Engine => "[Engine]"u8,
                LogScope.Assets => "[Asset]"u8,
                LogScope.Ecs => "[ECS]"u8,
                LogScope.Renderer => "[Render]"u8,
                LogScope.Gfx => "[GFX]"u8,
                LogScope.Backend => "[Driv]"u8,
                LogScope.Editor => "[Editor]"u8,
                _ => "[Unknown]"u8
            };
        }
    }
}