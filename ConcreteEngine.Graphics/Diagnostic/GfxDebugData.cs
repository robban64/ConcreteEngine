#region

using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

public readonly struct GfxStoreMetricsPayload(
    in GfxStoreMetricsRecord fk,
    in GfxStoreMetricsRecord bk,
    ResourceKind kind)
{
    public readonly GfxStoreMetricsRecord Fk = fk;
    public readonly GfxStoreMetricsRecord Bk = bk;
    public readonly ResourceKind Kind = kind;
}

public readonly record struct GfxStoreMetricsRecord(
    int Count,
    int Alive,
    int Free,
    int Capacity,
    in GfxMetaSpecialMetric Special);

public readonly record struct GfxMetaSpecialMetric(
    long Value,
    int ResourceId,
    ushort Param2 = 0,
    ResourceKind Kind = 0);


/*

public readonly record struct GfxDebuggLog(
    int HandleId = 0,
    int OtherValue = 0,
    ushort Gen = 0,
    ResourceKind Kind = ResourceKind.Invalid,
    GfxLogLayer Layer = GfxLogLayer.Unknown,
    GfxLogSource Source = GfxLogSource.Unknown,
    GfxLogAction Action = GfxLogAction.None)
{
    public long Time { get; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    private string MakeInfo(string idLabel)
    {
        var info = "";

        if (HandleId != -1)
            info = $"{idLabel}={HandleId.ToString(),-2}";

        if (Gen != 0)
            info = info.Length == 0 ? $"Gen={Gen.ToString(),-2}" : $"{info} Gen={Gen.ToString(),-2}";

        if (OtherValue != -1)
            info = info.Length == 0 ? $"Slot={OtherValue.ToString(),-2}" : $"{info} Slot={OtherValue.ToString(),-2}";

        return info.Length > 0 ? $" {{{info}}}" : string.Empty;
    }

    public string ToDebugStringInternal(string info)
    {
        var kindName = Kind.ToResourceName();
        kindName = kindName.Length > 10 ? kindName.Substring(0, 10) : kindName;

        var t = DateTimeOffset.FromUnixTimeMilliseconds(Time).ToLocalTime();
        var layerSource = $"{Layer.ToLogName()}-{Source.ToLogName()}".PadRight(18);
        var actionKind = $"{Action.ToLogName(),-10}{kindName,-10}";
        return $"[{t:HH:mm:ss.fff}] {layerSource} {actionKind}{info}";
    }

    public string ToDebugGfxString() => ToDebugStringInternal(MakeInfo("StoreId "));
    public string ToDebugBackendString() => ToDebugStringInternal(MakeInfo("GLHandle"));
    public string ToDebugUnknownString() => ToDebugStringInternal(MakeInfo("Id"));

    public string ToDebugString()
    {
        return Layer switch
        {
            GfxLogLayer.Gfx => ToDebugGfxString(),
            GfxLogLayer.Backend => ToDebugBackendString(),
            _ => ToDebugUnknownString()
        };
    }
}*/