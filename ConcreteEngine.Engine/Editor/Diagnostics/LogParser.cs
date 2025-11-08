#region

using System.Globalization;
using System.Text;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Engine.Editor.Diagnostics;

internal sealed class LogParser
{
    private readonly StringBuilder _sb = new(128);
    private readonly List<string> _buffer = new(8);

    public string Format(in LogEvent log)
    {
        return log.Scope switch
        {
            LogScope.Engine => ToBaseFormat(in log, id: "Id"),
            LogScope.Assets => ToBaseFormat(in log, id: "AssetId"),
            LogScope.World => ToBaseFormat(in log, id: "World"),
            LogScope.Renderer => ToBaseFormat(in log, id: "RendererId"),
            LogScope.Backend => ToBaseFormat(in log, id: "Handle", p0: "Slot", p1: "Alive"),
            LogScope.Gfx => ToBaseFormat(in log, id: "GfxId", p0: "Slot", p1: "Alive"),
            _ => ToBaseFormat(in log, id: "Id", p0: "P0", p1: "P1", fp: "F0", flags: "Flags")
        };
    }

    public string Format(StringLogEvent log)
    {
        _sb.Clear();

        var t = DateTimeOffset.FromUnixTimeMilliseconds(log.Time).ToLocalTime();
        _sb.Append('[').Append(log.Level.ToLogText()).Append("] [")
            .Append(t.ToString("HH:mm:ss.fff")).Append("] ")
            .Append(log.Scope.ToLogText().PadLeft(10)).Append(": ")
            .Append(log.Message);

        return _sb.ToString();
    }

    private string ToBaseFormat(
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

        var t = DateTimeOffset.FromUnixTimeMilliseconds(log.Time).ToLocalTime();
        _sb.Append('[').Append(log.Level.ToLogText()).Append("] [")
            .Append(t.ToString("HH:mm:ss.fff")).Append("] ")
            .Append(log.Scope.ToLogText().PadLeft(10)).Append(": ")
            .Append(log.Action.ToLogText().PadRight(4)).Append('-').Append(log.Topic.ToLogText().PadRight(4))
            .Append(' ').Append(id).Append('=').Append(log.Id)
            .Append(" Gen=").Append(log.Gen).Append(" { ");

        if (p0 is not null) _buffer.Add($"{p0}={log.Param0.ToString(),-2}");
        if (p1 is not null) _buffer.Add($"{p1}={log.Param1.ToString(),-2}");
        if (fp is not null) _buffer.Add($"{fp}={log.FParam0.ToString(CultureInfo.InvariantCulture),-2}");
        if (gen is not null) _buffer.Add($"{gen}={log.Gen.ToString(),2}");
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