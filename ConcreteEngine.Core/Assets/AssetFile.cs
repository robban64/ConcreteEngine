namespace ConcreteEngine.Core.Assets;

public enum AssetFileType
{
    Texture2D,
    Shader,
    Mesh,
    Material
}

public interface IAssetFile
{
    string Name { get; init; }
    AssetFileType AssetType { get; }
}

public interface IGraphicAssetFile<THandle> : IAssetFile where THandle : unmanaged
{
    public THandle ResourceId { get; init; }
}