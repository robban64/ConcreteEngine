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
        string info = "";

        if (HandleId != -1) info = $"{idLabel}={HandleId.ToString(),-2}";
        if (Gen != 0) info = info.Length == 0 ? $"Gen={Gen.ToString(),-2}" : $"{info} Gen={Gen.ToString(), -2}";
        if (OtherValue != -1) info = info.Length == 0 ? $"Slot={OtherValue.ToString(),-2}" : $"{info} Slot={OtherValue.ToString(), -2}";

        return info.Length > 0 ? $" {{{info}}}" : string.Empty;
    }

    public string ToDebugStringInternal(string info)
    {
        var kindName = Kind.ToLogName();
        kindName = kindName.Length > 10 ? kindName.Substring(0, 10) : kindName;
        
        var t = DateTimeOffset.FromUnixTimeMilliseconds(Time).ToLocalTime();
        var layerSource = $"{Layer.ToLogName()}-{Source.ToLogName()}".PadRight(18);
        var actionKind  = $"{Action.ToLogName(),-10}{kindName,-10}";
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

public enum GfxLogSource : byte
{
    Unknown,
    Store,
    Resource,
    Shader,
    Program,
    Framebuffer,
    State,
    Command,
    Validation,
    Mesh,     
    Buffer 

}

public enum GfxLogAction : byte
{
    None,
    Add,
    Remove,
    Replace,

    Compile,
    Link,

    Dispose,
    EnqueueDispose,

    Bind,
    Unbind,
    SetState,
    Invalidate,
    Validate,
    Notify,

    Upload,
    Map
}

public enum GfxLogLayer : byte
{
    Unknown,
    Misc,
    Gfx,
    Backend
}

public static class GfxLogExtensions
{
    public static string ToLogName(this GfxLogSource value)
    {
        return value switch
        {
            GfxLogSource.Store        => "Store",
            GfxLogSource.Resource     => "Resource",
            GfxLogSource.Shader       => "Shader",
            GfxLogSource.Program      => "Program",
            GfxLogSource.Framebuffer  => "FBO",
            GfxLogSource.State        => "State",
            GfxLogSource.Command      => "Command",
            GfxLogSource.Validation   => "Validation",
            GfxLogSource.Mesh         => "Mesh",
            GfxLogSource.Buffer       => "Buffer",
            _                         => "Unknown"
        };
    }

    public static string ToLogName(this GfxLogAction value)
    {
        return value switch
        {
            GfxLogAction.Add            => "Add",
            GfxLogAction.Remove         => "Remove",
            GfxLogAction.Replace        => "Replace",
            GfxLogAction.Compile        => "Compile",
            GfxLogAction.Link           => "Link",
            GfxLogAction.Dispose        => "Dispose",
            GfxLogAction.EnqueueDispose => "EnqDispose",
            GfxLogAction.Bind           => "Bind",
            GfxLogAction.Unbind         => "Unbind",
            GfxLogAction.SetState       => "SetState",
            GfxLogAction.Invalidate     => "Invalidate",
            GfxLogAction.Validate       => "Validate",
            GfxLogAction.Notify         => "Notify",
            GfxLogAction.Upload         => "Upload",
            GfxLogAction.Map            => "Map",
            _                           => "None"
        };
    }

    public static string ToLogName(this GfxLogLayer value)
    {
        return value switch
        {
            GfxLogLayer.Misc => "Misc",
            GfxLogLayer.Gfx => "Gfx",
            GfxLogLayer.Backend => "Backend",
            _ => "Unknown"
        };
    }

    public static string ToLogName(this ResourceKind kind, bool shortName = false)
    {
        if (shortName)
        {
            return kind switch
            {
                ResourceKind.Texture => "TEX",
                ResourceKind.Shader => "SHD",
                ResourceKind.Mesh => "MSH",
                ResourceKind.VertexBuffer => "VBO",
                ResourceKind.IndexBuffer => "IBO",
                ResourceKind.UniformBuffer => "UBO",
                ResourceKind.FrameBuffer => "FBO",
                ResourceKind.RenderBuffer => "RBO",
                _ => "INV"
            };
        }

        return kind switch
        {
            ResourceKind.Texture => "Texture",
            ResourceKind.Shader => "Shader",
            ResourceKind.Mesh => "Mesh",
            ResourceKind.VertexBuffer => "VertexBuffer",
            ResourceKind.IndexBuffer => "IndexBuffer",
            ResourceKind.UniformBuffer => "UniformBuffer",
            ResourceKind.FrameBuffer => "FrameBuffer",
            ResourceKind.RenderBuffer => "RenderBuffer",
            _ => "Invalid"
        };
    }
}