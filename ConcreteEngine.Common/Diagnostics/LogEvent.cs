namespace ConcreteEngine.Common.Diagnostics;

public readonly record struct LogEvent(
    uint Id,
    int Param0,
    int Param1 = 0,
    float FParam0 = 0,
    ushort Gen = 0,
    ushort Flags = 0,
    LogTopic Topic = LogTopic.Unknown,
    LogScope Scope = LogScope.Unknown,
    LogAction Action = LogAction.Unknown,
    LogLevel Level = LogLevel.Info)
{
    public long Time { get; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public static implicit operator uint(LogEvent log) => log.Id;

    public string ToBaseFormat(string? p0 = null, string? p1 = null, string? fp = null, string? gen = null,
        string? flags = null)
    {
        var t = DateTimeOffset.FromUnixTimeMilliseconds(Time).ToLocalTime();
        var head = $"[{Level.ToLogText()}] [{t:HH:mm:ss.fff}] {Scope.ToLogText()}:";
        var subj = $"{Action.ToLogText()}-{Topic.ToLogText()}";
        var id = $"Id={Id} Gen={Gen}";

        var bodyParts = new List<string>(5);
        if (p0 is not null) bodyParts.Add($"{p0}={Param0}");
        if (p1 is not null) bodyParts.Add($"{p1}={Param1}");
        if (fp is not null) bodyParts.Add($"{fp}={FParam0}");
        if (gen is not null) bodyParts.Add($"{gen}={Gen}");
        if (flags is not null) bodyParts.Add($"{flags}={Flags}");

        var body = bodyParts.Count > 0
            ? string.Join("; ", bodyParts)
            : string.Empty;

        return $"{head} {subj}{id} {{ {body} }}";
    }
}

public readonly record struct LogFilterWildcard(byte Topic, byte Scope, byte Action, byte Level)
{
    public LogFilterWildcard(LogTopic Topic, LogScope Scope, LogAction Action, LogLevel Level)
        : this((byte)Topic, (byte)Scope, (byte)Action, (byte)Level)
    {
    }
}