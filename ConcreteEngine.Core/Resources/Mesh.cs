using ConcreteEngine.Core.Assets;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Resources;

public sealed class Mesh : IGraphicAssetFile<MeshId>
{
    public string Name { get; init; }

    public string? Filename { get; init; }

    public bool IsStatic { get; init; }

    public int DrawCount { get; init; }

    public AssetKind AssetType => AssetKind.Mesh;
    public ResourceKind GfxResourceKind => ResourceKind.Mesh;
    public MeshId ResourceId { get; init; }
}