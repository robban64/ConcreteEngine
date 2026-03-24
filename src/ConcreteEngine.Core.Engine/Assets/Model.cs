using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class MeshEntry(string name, MeshInfo info)
{
    public readonly string Name = name;
    public MeshId MeshId;
    public MeshInfo Info = info;
    public Matrix4x4 WorldTransform;
    public BoundingBox LocalBounds;
}

public sealed class ModelAssetRefs(AssetIndexRef[] materialIndices, AssetIndexRef[] textureIndices)
{
    public readonly AssetIndexRef[] MaterialIndices = materialIndices;
    public readonly AssetIndexRef[] TextureIndices = textureIndices;
}

public sealed class Model : AssetObject
{
    public MeshEntry[] Meshes { get; }
    public ModelAssetRefs AssetRefs { get; }
    public ModelAnimation? Animation { get; }

    public readonly ModelInfo Info;
    public readonly BoundingBox Bounds;

    //
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;


    public Model(
        string name,
        in ModelInfo modelInfo,
        in BoundingBox bounds,
        MeshEntry[] meshes,
        ModelAnimation? animation,
        ModelAssetRefs assetRefs) : base(name)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(assetRefs);
        ArgumentOutOfRangeException.ThrowIfNotEqual(meshes.Length, modelInfo.MeshCount);

        Info = modelInfo;
        Bounds = bounds;
        Meshes = meshes;
        Animation = animation;
        AssetRefs = assetRefs;
    }


    public void AttachAnimation(AnimationId animationId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(animationId.Value, 0, nameof(animationId));
        if (Animation is null) throw new InvalidOperationException("Animation is null");
        InvalidOpThrower.ThrowIf(Animation.AnimationId.Value > 0, nameof(ModelId));

        Animation.AnimationId = animationId;
    }

    internal override AssetObject CopyAndIncreaseGen() => throw new NotImplementedException();
}