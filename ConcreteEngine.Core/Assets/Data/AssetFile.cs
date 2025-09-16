using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Resources;

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

public interface IGraphicAssetFile : IAssetFile
{
    ResourceKind GfxResourceKind { get; }
}

public interface IGraphicAssetFile<out THandle> : IGraphicAssetFile where THandle : unmanaged
{
    THandle ResourceId { get; }
}