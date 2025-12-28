using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Extensions;
using ConcreteEngine.Core.Diagnostics.Logging;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.CLI;

internal static class LogParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> Format(Span<char> buffer, StringLogEvent log)
    {
        if (log.IsPlain()) return log.Message;
        var zaBuilder = ZaSpanStringBuilder.Create(buffer);

        zaBuilder.Append('[').Append(log.Level.ToLogText())
            .Append("] [").Append(log.Timestamp, "HH:mm:ss.fff").Append("] ")
            .Append(log.Scope.ToLogText()).Append(":    ")
            .Append(log.Message);

        return zaBuilder.AsSpan();
    }
}