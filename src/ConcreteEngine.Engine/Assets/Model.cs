using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Assets.Loader.ImporterModel;

namespace ConcreteEngine.Engine.Assets;

public sealed record Model : AssetObject, IModel
{
    private static ModelId CreateModelId() => new(++_modelIdx);
    private static int _modelIdx;

    public ModelId ModelId { get; } = CreateModelId();
    public AnimationId AnimationId { get; private set; }
    
    public required int VertexCount { get; init; }
    public required int FaceCount { get; init; }

    public required BoundingBox Bounds { get; init; }

    public required MeshEntry[] Meshes { get; init; }
    public required Matrix4x4[] WorldTransforms { get; init; }
    
    public required ModelAnimation? Animation { get; init; }

    //
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;

    //
    public int MeshCount => Meshes.Length;
    public bool IsAnimated => Animation?.AnimationCount > 0;

    public void AttachAnimation(AnimationId animationId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(animationId.Value, 0, nameof(animationId));
        InvalidOpThrower.ThrowIf(AnimationId.Value > 0, nameof(ModelId));
        InvalidOpThrower.ThrowIfNull(Animation);

        AnimationId = animationId;
    }
}