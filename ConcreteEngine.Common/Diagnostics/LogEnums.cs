namespace ConcreteEngine.Common.Diagnostics;

public enum LogLevel : byte
{
    Unset = 0,
    Trace = 1,
    Debug = 2,
    Info = 3,
    Warn = 4,
    Error = 5,
    Critical = 6
}

public enum LogTopic : byte
{
    Unknown = 0,
    Texture = 1,
    Shader = 2,
    Mesh = 3,
    VertexBuffer = 4,
    IndexBuffer = 5,
    UniformBuffer = 6,
    FrameBuffer = 7,
    RenderBuffer = 8,
    Material = 9,
    Asset = 10,
    Renderer = 11,
    Frame = 12,
    Pass = 13,
    CommandList = 14,
    Pipeline = 15,
}

public enum LogScope : byte
{
    Unknown = 0,
    Backend = 1,
    Gfx = 2,
    Renderer = 3,
    Engine = 4,
    BkStore = 5,
    GfxStore = 6,
    MaterialStore = 7,
    AssetStore = 8,
}

public enum LogAction : byte
{
    Unknown = 0,
    Add = 1,
    Remove = 2,
    Replace = 3,
    Create = 4,
    Destroy = 5,
    Bind = 6,
    Unbind = 7,
    Upload = 8,
    Download = 9,
    Map = 10,
    Unmap = 11,
    Compile = 12,
    Link = 13,
    Begin = 14,
    End = 15,
    Submit = 16,
    Resize = 17,
    Evict = 18,
    Execute = 19,
    EnqRemove = 20,
    EnqReplace = 21,
}