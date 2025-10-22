using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Graphics.Diagnostic;


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
            GfxLogSource.Store => "Store",
            GfxLogSource.Resource => "Resource",
            GfxLogSource.Shader => "Shader",
            GfxLogSource.Program => "Program",
            GfxLogSource.Framebuffer => "FBO",
            GfxLogSource.State => "State",
            GfxLogSource.Command => "Command",
            GfxLogSource.Validation => "Validation",
            GfxLogSource.Mesh => "Mesh",
            GfxLogSource.Buffer => "Buffer",
            _ => "Unknown"
        };
    }

    public static string ToLogName(this GfxLogAction value)
    {
        return value switch
        {
            GfxLogAction.Add => "Add",
            GfxLogAction.Remove => "Remove",
            GfxLogAction.Replace => "Replace",
            GfxLogAction.Compile => "Compile",
            GfxLogAction.Link => "Link",
            GfxLogAction.Dispose => "Dispose",
            GfxLogAction.EnqueueDispose => "EnqDispose",
            GfxLogAction.Bind => "Bind",
            GfxLogAction.Unbind => "Unbind",
            GfxLogAction.SetState => "SetState",
            GfxLogAction.Invalidate => "Invalidate",
            GfxLogAction.Validate => "Validate",
            GfxLogAction.Notify => "Notify",
            GfxLogAction.Upload => "Upload",
            GfxLogAction.Map => "Map",
            _ => "None"
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