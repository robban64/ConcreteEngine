namespace ConcreteEngine.Core.Engine.Assets;


[Flags]
public enum AssetDirtyFlag : byte
{
    None = 0,
    Name = 1 << 0,
    Metadata = 1 << 1,
    State = 1 << 2,
    Structure = 1 << 3,
    Dependencies = 1 << 4,
    Lifecycle = 1 << 5,
}

public enum AssetKind : byte
{
    Unknown = 0,
    Shader = 1,
    Model = 2,
    Texture = 3,
    Material = 4,
}

public enum AssetCategory : byte
{
    Unknown = 0,
    Graphic = 1,
    Renderer = 2,
    Data = 3,
    Audio = 4,
    Script = 5
}

public enum AssetStorage : byte
{
    None = 0,
    FileSystem = 1,
    InMemory = 2,
    Package = 2,
    Embedded = 3
}

public enum AssetLoadingMode : byte
{
    Ignore = 0,
    Processed = 1,
    MemoryOnly = 2
}

public enum AssetProcessStatus : byte
{
    Done,
    Failed
}