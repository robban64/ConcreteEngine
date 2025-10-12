#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

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

public interface IAssetFile
{
    string Name { get; init; }
    AssetKind AssetType { get; }
}

public interface IGraphicAssetFile : IAssetFile
{
    ResourceKind GfxResourceKind { get; }
}

public interface IGraphicAssetFile<out TId> : IGraphicAssetFile where TId : unmanaged, IResourceId
{
    TId ResourceId { get; }
}