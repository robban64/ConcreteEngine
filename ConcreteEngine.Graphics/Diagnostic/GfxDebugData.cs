using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Diagnostic;

public readonly record struct GfxDebugLog(
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
        var kindName = Kind.ToLogName();
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
}
