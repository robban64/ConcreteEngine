using System.Globalization;
using System.Text;
using ConcreteEngine.Core.Diagnostics.Extensions;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Editor.CLI;

internal static class StructLogParser
{
    private static readonly StringBuilder _sb = new(128);
    private static readonly List<string> _buffer = new(8);

    public static string GetLogMessage(in LogEvent log)
    {
        var message = log.Scope switch
        {
            LogScope.Engine => ToBaseFormat(in log, id: "Id"),
            LogScope.Assets => ToBaseFormat(in log, id: "AssetId"),
            LogScope.World => ToBaseFormat(in log, id: "World"),
            LogScope.Renderer => ToBaseFormat(in log, id: "RendererId"),
            LogScope.Backend => ToBaseFormat(in log, id: "Handle", p0: "Slot", p1: "Alive"),
            LogScope.Gfx => ToBaseFormat(in log, id: "GfxId", p0: "Slot", p1: "Alive"),
            _ => ToBaseFormat(in log, id: "Id", p0: "P0", p1: "P1", fp: "F0", flags: "Flags")
        };

        return message;
    }

    private static string ToBaseFormat(
        in LogEvent log,
        string id = "Id",
        string? p0 = null,
        string? p1 = null,
        string? fp = null,
        string? gen = null,
        string? flags = null)
    {
        _buffer.Clear();
        _sb.Clear();

        _sb.Append(log.Action.ToLogText().PadRight(4)).Append('-').Append(log.Topic.ToLogText().PadRight(4))
            .Append(' ').Append(id).Append('=').Append(log.Id)
            .Append(" Gen=").Append(log.Gen).Append(" { ");

        if (p0 is not null) _buffer.Add($"{p0}={log.Param0,-2}");
        if (p1 is not null) _buffer.Add($"{p1}={log.Param1,-2}");
        if (fp is not null) _buffer.Add($"{fp}={log.FParam0.ToString(CultureInfo.InvariantCulture),-2}");
        if (gen is not null) _buffer.Add($"{gen}={log.Gen,2}");
        if (flags is not null) _buffer.Add($"{flags}={log.Flags}");

        for (int i = 0; i < _buffer.Count; i++)
        {
            if (i > 0) _sb.Append("; ");
            _sb.Append(_buffer[i]);
        }

        _sb.Append(" }");
        return _sb.ToString();
    }
}