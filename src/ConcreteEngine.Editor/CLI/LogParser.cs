using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Core.Diagnostics.Extensions;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.CLI;

internal sealed class LogParser
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> Format(Span<char> buffer, StringLogEvent log)
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