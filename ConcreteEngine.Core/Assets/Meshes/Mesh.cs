#region

using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Meshes;

public sealed class Mesh : AssetObject
{
    public AssetRef<Mesh> RefId => new(RawId);

    public MeshId ResourceId { get; init; }

    public int DrawCount { get; init; }

    public override AssetKind Kind => AssetKind.Mesh;
    public override AssetCategory Category => AssetCategory.Graphic;
    public ResourceKind GfxResourceKind => ResourceKind.Mesh;
}