using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Extensions;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.CLI;

internal static class LogParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> Format(ref SpanWriter sw, StringLogEvent log)
    {
        if (log.IsPlain())
        {
            
            return sw.Write(log.Message);
        }

        var ts = log.Timestamp;

        return sw
            .Start("["u8).Append(log.Level.ToLogText())
            .Append("] ["u8)
            .Append(ts.Hour).Append(":"u8).Append(ts.Minute)
            .Append(":"u8).Append(ts.Second).Append(":"u8).Append(ts.Millisecond)
            .Append("] "u8)
            .Append(log.Scope.ToLogText()).Append(": "u8)
            .Append(log.Message)
            .End();
    }
}