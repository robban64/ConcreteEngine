#region

using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;

#endregion

namespace ConcreteEngine.Engine.Assets.Models;

public readonly record struct ModelBaseDrawInfo(ModelId Model, int PartCount, int DrawCount);

public sealed class Model : AssetObject, IComparable<Model>
{
    public ModelId ModelId { get; private set; }
    public required int DrawCount { get; init; }
    public required BoundingBox Bounds { get; init; }

    public required ModelMesh[] MeshParts { get; init; }
    public required ModelAnimation? Animation { get; init; }

    //
    public AssetRef<Model> RefId => new(RawId);
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;
    public ResourceKind GfxResourceKind => ResourceKind.Mesh;

    //
    public ModelBaseDrawInfo ToBaseDrawInfo() => new(ModelId, MeshParts.Length, DrawCount);

    internal void AttachToRenderer(ModelId modelId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(modelId.Value, 0, nameof(modelId));
        InvalidOpThrower.ThrowIf(ModelId.Value > 0, nameof(ModelId));
        ModelId = modelId;
    }

    public int CompareTo(Model? other)
    {
        return other is null ? 1 : RawId.Value.CompareTo(other.RawId.Value);
    }
}