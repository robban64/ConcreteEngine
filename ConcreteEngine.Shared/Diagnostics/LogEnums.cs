namespace ConcreteEngine.Shared.Diagnostics;

[Flags]
public enum LogFilter : ushort
{
    LogLevel = 0,
    LogTopic = 1 << 0,
    LogScope = 1 << 1,
    LogAction = 1 << 2
}

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
    Io = 10,
    Renderer = 11,
    Frame = 12,
    Pass = 13,
    CommandList = 14,
    Pipeline = 15,
    Core = 16,
    ArrayBuffer = 17
}

public enum LogScope : byte
{
    Unknown = 0,
    Engine = 1,
    Assets = 2,
    World = 3,
    Renderer = 4,
    Gfx = 5,
    Backend = 6
}

public enum LogAction : byte
{
    Unknown = 0,
    Load = 1,
    Unload = 2,
    Add = 3,
    Remove = 4,
    Replace = 5,
    Create = 6,
    Destroy = 7,
    Bind = 8,
    Unbind = 9,
    Upload = 10,
    Download = 11,
    Map = 12,
    Unmap = 13,
    Compile = 14,
    Link = 15,
    Begin = 16,
    End = 17,
    Submit = 18,
    Resize = 19,
    Evict = 20,
    Execute = 21,
    EnqRemove = 22,
    EnqReplace = 23
}