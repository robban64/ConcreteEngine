namespace ConcreteEngine.Core.Assets;

public enum AssetFileType
{
    Texture2D,
    Shader
}

public interface IAssetFile
{
    string Name { get; init; }
    AssetFileType AssetType { get; }
}

public interface IGraphicAssetFile : IAssetFile
{
    public ushort ResourceId { get; init; }
}