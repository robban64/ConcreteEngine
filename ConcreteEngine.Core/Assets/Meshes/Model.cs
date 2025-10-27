#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Core.Assets.Data;
using ConcreteEngine.Core.RenderingSystem.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Assets.Meshes;

public sealed class Model : AssetObject,  IComparable<Model>
{
    public ModelId RenderId { get; private set; } = default;
    public required ModelMesh[] MeshParts { get; init; }
    public required int DrawCount { get; init; }

    
    public AssetRef<Model> RefId => new(RawId);
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;
    public ResourceKind GfxResourceKind => ResourceKind.Mesh;

    internal void AttachToRenderer(ModelId modelId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(modelId.Value, 0, nameof(modelId));
        InvalidOpThrower.ThrowIf(RenderId.Value > 0, nameof(RenderId));
        RenderId = modelId;
    }
    
    public int CompareTo(Model? other)
    {
        return other is null ? 1 : RawId.Value.CompareTo(other.RawId.Value);
    }
}

public sealed record ModelMesh(
    AssetRef<Model> AssetRef,
    string MeshName,
    MeshId ResourceId,
    int MaterialSlot,
    int DrawCount,
    Matrix4x4 Transform);