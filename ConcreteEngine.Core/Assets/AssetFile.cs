namespace ConcreteEngine.Core.Assets;

public enum AssetFileType
{
    Texture2D,
    Shader,
    Mesh,
    Material,
    Cubemap
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