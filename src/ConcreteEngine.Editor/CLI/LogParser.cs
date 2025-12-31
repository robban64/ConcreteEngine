using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Extensions;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Utils;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.CLI;

internal static class LogParser
{
    public static string TimeFormat = "HH:mm:ss.fff";


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> Format(Span<char> buffer, StringLogEvent log)
    {
        var zaBuilder = ZaSpanStringBuilder.Create(buffer);

        if (log.IsPlain()) return zaBuilder.Append(log.Message).AsRawBytes();

        zaBuilder
            .Append('[').Append(log.Level.ToLogText())
            .Append("] [").Append(log.Timestamp, TimeFormat).Append("] ")
            .PadRight(log.Scope.ToLogText(), ":", 8)
            .Append(log.Message);

        return zaBuilder.AsRawBytes();
    }
}