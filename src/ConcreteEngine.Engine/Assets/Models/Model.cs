using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Worlds.Data;

namespace ConcreteEngine.Engine.Assets.Models;

public readonly struct ModelMeshInfo(ModelId model, AnimationId animation, int partCount, int drawCount)
{
    public readonly int DrawCount = drawCount;
    public readonly ModelId Model = model;
    public readonly AnimationId Animation = animation;
    public readonly byte PartCount = (byte)partCount;
}

public sealed class Model : AssetObject, IComparable<Model>
{
    public ModelId ModelId { get; private set; }
    public AnimationId AnimationId { get; private set; }
    public required int DrawCount { get; init; }
    public required BoundingBox Bounds { get; init; }

    public required ModelMesh[] MeshParts { get; init; }
    public required ModelAnimation? Animation { get; init; }

    //
    public AssetRef<Model> RefId => new(RawId);
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;
    public GraphicsKind GraphicsKind => GraphicsKind.Mesh;

    public bool IsAnimated => Animation?.ClipDataSpan.Length > 0 && AnimationId > 0;

    //
    public ModelMeshInfo ToBaseDrawInfo() => new(ModelId, AnimationId, (byte)MeshParts.Length, DrawCount);

    internal void AttachModel(ModelId modelId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(modelId.Value, 0, nameof(modelId));
        InvalidOpThrower.ThrowIf(ModelId.Value > 0, nameof(ModelId));
        ModelId = modelId;
    }

    public void AttachAnimation(AnimationId animationId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(animationId.Value, 0, nameof(animationId));
        InvalidOpThrower.ThrowIf(AnimationId.Value > 0, nameof(ModelId));
        InvalidOpThrower.ThrowIfNull(Animation);

        Animation!.Attach(animationId);
        AnimationId = animationId;
    }

    public int CompareTo(Model? other)
    {
        return other is null ? 1 : RawId.Value.CompareTo(other.RawId.Value);
    }
}