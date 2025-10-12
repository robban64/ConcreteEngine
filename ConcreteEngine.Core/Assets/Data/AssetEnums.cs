namespace ConcreteEngine.Core.Assets;

public enum AssetKind
{
    Unknown = 0,
    Shader = 1,
    Mesh = 2,
    Texture2D = 3,
    CubeMap = 4,
    Material = 5
}

public enum AssetCategory
{
    Unknown = 0,
    Graphic = 1,
    Data = 2,
    Audio = 3,
    Script = 4,
}

public enum AssetStorageKind
{
    Unknown = 0,
    FileSystem = 1,
    Package = 2,
    Embedded = 3
}