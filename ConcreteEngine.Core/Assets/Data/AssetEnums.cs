namespace ConcreteEngine.Core.Assets.Data;

public enum AssetKind
{
    Unknown = 0,
    Shader = 1,
    Mesh = 2,
    Texture2D = 3,
    TextureCubeMap = 4,
    Material = 5
}

public enum AssetCategory
{
    Unknown = 0,
    Graphic = 1,
    Renderer = 2,
    Data = 3,
    Audio = 4,
    Script = 5,
}

public enum AssetStorageKind
{
    Unknown = 0,
    FileSystem = 1,
    Package = 2,
    Embedded = 3
}

public enum AssetProcessStatus
{
    Done,
    Failed
}