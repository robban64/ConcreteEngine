using System.Globalization;
using System.Text;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Extensions;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Editor.CLI;

internal static class StructLogParser
{
    public static ReadOnlySpan<byte> GetLogMessage(UnsafeSpanWriter sw, in LogEvent log)
    {
        var message = log.Scope switch
        {
            LogScope.Engine => ToBaseFormat(sw, in log, id: "Id"),
            LogScope.Assets => ToBaseFormat(sw, in log, id: "AssetId"),
            LogScope.World => ToBaseFormat(sw, in log, id: "World"),
            LogScope.Renderer => ToBaseFormat(sw, in log, id: "RendererId"),
            LogScope.Backend => ToBaseFormat(sw, in log, id: "Handle", p0: "Slot", p1: "Alive"),
            LogScope.Gfx => ToBaseFormat(sw, in log, id: "GfxId", p0: "Slot", p1: "Alive"),
            _ => ToBaseFormat(sw, in log, id: "Id", p0: "P0", p1: "P1", fp: "F0", flags: "Flags")
        };

        return message;
    }

    private static ReadOnlySpan<byte> ToBaseFormat(
        UnsafeSpanWriter sw,
        in LogEvent log,
        string id = "Id",
        string? p0 = null,
        string? p1 = null,
        string? fp = null,
        string? gen = null,
        string? flags = null)
    {
        sw.Clear();
        sw.Append(log.Action.ToLogText().PadRight(4)).Append('-').Append(log.Topic.ToLogText().PadRight(4))
            .Append(' ').Append(id).Append('=').Append(log.Id)
            .Append(" Gen=").Append(log.Gen).Append(" { ");

        if (p0 is not null) sw.Append($"{p0}={log.Param0,-2}; ");
        if (p1 is not null) sw.Append($"{p1}={log.Param1,-2}; ");
        if (fp is not null) sw.Append($"{fp}={log.FParam0.ToString(CultureInfo.InvariantCulture),-2}; ");
        if (gen is not null) sw.Append($"{gen}={log.Gen,2}; ");
        if (flags is not null) sw.Append($"{flags}={log.Flags}; ");

        sw.Append(" }");
        return sw.EndSpan();
    }
}