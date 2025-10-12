#region

using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Resources;

public sealed class Mesh : IGraphicAssetFile<MeshId>
{
    public string Name { get; init; }

    public string? Filename { get; init; }

    public bool IsStatic { get; init; }

    public int DrawCount { get; init; }

    public AssetKind Kind => AssetKind.Mesh;
    public ResourceKind GfxResourceKind => ResourceKind.Mesh;
    public MeshId ResourceId { get; init; }
}