using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Extensions;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Utils;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.CLI;

internal static class LogParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> Format(Span<byte> buffer, StringLogEvent log)
    {
        var zaBuilder = ZaUtf8SpanWriter.Create(buffer);
        zaBuilder.Clear();

        if (log.IsPlain())
        {
            zaBuilder.AppendEnd(log.Message);
            return zaBuilder.AsSpan();
        }

        var ts = log.Timestamp;

        zaBuilder
            .Append("["u8).Append(log.Level.ToLogText())
            .Append("] ["u8)
            .AppendFormat0(ts.Hour).Append(":"u8).AppendFormat0(ts.Minute)
            .Append(":"u8).AppendFormat0(ts.Second).Append(":"u8).AppendFormat0(ts.Millisecond)
            .Append("] "u8)
            .Append(log.Scope.ToLogText()).Append(": "u8)
            .AppendEnd(log.Message);

        return zaBuilder.AsSpan();
    }
}