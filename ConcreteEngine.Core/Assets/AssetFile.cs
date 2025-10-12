#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets;



public interface IAssetFile
{
    string Name { get; init; }
    AssetKind Kind { get; }
}

public interface IGraphicAssetFile : IAssetFile
{
    ResourceKind GfxResourceKind { get; }
}

public interface IGraphicAssetFile<out TId> : IGraphicAssetFile where TId : unmanaged, IResourceId
{
    TId ResourceId { get; }
}