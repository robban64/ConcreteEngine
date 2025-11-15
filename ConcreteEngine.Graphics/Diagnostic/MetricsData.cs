#region

using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Graphics.Diagnostic;

public struct GfxStoreMetricsPayload(
    in CollectionSample fk,
    in CollectionSample bk,
    in TargetMetric specialMetric,
    in ValueSample specialSample,
    ResourceKind kind)
{
    public CollectionSample Fk = fk;
    public CollectionSample Bk = bk;
    public TargetMetric SpecialMetric = specialMetric;
    public ValueSample SpecialSample = specialSample;
    public ResourceKind Kind = kind;
}

public readonly struct GfxMetaSpecialMetric(
    long value,
    int resourceId,
    ushort param2 = 0,
    ResourceKind kind = 0)
{
    public readonly long Value = value;
    public readonly int ResourceId = resourceId;
    public readonly ushort Param2 = param2;
    public readonly ResourceKind Kind = kind;
}

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