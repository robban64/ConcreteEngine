using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Assets.Models;

namespace ConcreteEngine.Engine.Assets;

public sealed record Model : AssetObject, IModel
{
    public ModelId ModelId { get; private set; }
    public AnimationId AnimationId { get; private set; }

    public required int DrawCount { get; init; }
    public required BoundingBox Bounds { get; init; }

    public required ModelMesh[] Meshes { get; init; }
    public required ModelAnimation? Animation { get; init; }

    //
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;

    //
    public int MeshCount  => Meshes.Length;
    public bool IsAnimated => Animation?.ClipCount > 0;


    public void AttachModel(ModelId modelId)
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
}