namespace ConcreteEngine.Graphics;

public enum BufferUsage : byte
{
    StaticDraw = 0,
    DynamicDraw = 1,
    StreamDraw = 2
}

public enum BufferStorage : byte
{
    Static = 0,
    Dynamic = 1,
    Stream = 2
}

[Flags]
public enum BufferAccess : byte
{
    None   = 0,
    MapRead  = 1 << 0,
    MapWrite = 1 << 1,
    Persistent = 1 << 2,
    Coherent   = 1 << 3
}