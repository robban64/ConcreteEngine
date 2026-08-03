using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Extensions;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Editor.Logging;

internal static class ValueLogParser
{
    public static ReadOnlySpan<byte> GetLogMessage(NativeSpanWriter sw, in LogEvent log)
    {
        return log.Scope switch
        {
            LogScope.Engine => ToBaseFormat(sw, in log, id: "Id"),
            LogScope.Assets => ToBaseFormat(sw, in log, id: "AssetId"),
            LogScope.Ecs => ToBaseFormat(sw, in log, id: "World"),
            LogScope.Renderer => ToBaseFormat(sw, in log, id: "RendererId"),
            LogScope.Backend => ToBaseFormat(sw, in log, id: "Handle", p0: "Slot", p1: "Alive"),
            LogScope.Gfx => ToBaseFormat(sw, in log, id: "GfxId", p0: "Slot", p1: "Alive"),
            _ => ToBaseFormat(sw, in log, id: "Id", p0: "P0", p1: "P1", fp: "F0", flags: "Flags")
        };
    }

    private static ReadOnlySpan<byte> ToBaseFormat(
        NativeSpanWriter sw,
        in LogEvent log,
        string id = "Id",
        string? p0 = null,
        string? p1 = null,
        string? fp = null,
        string? gen = null,
        string? flags = null)
    {
        const byte eq = (byte)'=';

        var action = log.Action.ToLogText();
        var topic = log.Topic.ToLogText();

        sw.Clear();
        sw.Append(action).PadRight(4).AppendAscii('-');
        sw.Append(topic).PadRight(4).AppendAscii(' ');
        sw.Append(id).Append(eq).Append(log.Id);
        sw.Append(" Gen=").Append(log.Gen);
        sw.AppendAscii(' ', '{', ' ');

        if (p0 is not null)
        {
            sw.Append(p0).Append(eq);
            sw.Append($"{log.Param0,-2}");
        }

        if (p1 is not null)
        {
            sw.Append(p1).Append(eq);
            sw.Append($"{log.Param1,-2}");
        }

        if (fp is not null)
        {
            sw.Append(fp).Append(eq);
            sw.Append($"{log.FParam0,-2}");
        }

        if (gen is not null)
        {
            sw.Append(gen).Append(eq);
            sw.Append($"{log.Gen,2}");
        }

        if (flags is not null)
        {
            sw.Append(flags).Append(eq);
            sw.Append(log.Flags);
        }

        sw.AppendAscii(';', ' ');
        sw.AppendAscii(' ', '}');
        return sw.EndSpan();
    }
}