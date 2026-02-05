using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets;

public sealed class MeshEntry
{
    public readonly string Name;
    public MeshId MeshId;
    public MeshInfo Info;
    public BoundingBox LocalBounds;

    internal MeshEntry(string name, MeshInfo info)
    {
        Name = name;
        Info = info;
    }
}

public sealed class Model : AssetObject, IModel
{

    public Model(ModelInfo modelInfo, in BoundingBox bounds, MeshEntry[] meshes, Matrix4x4[] worldTransforms,
        ModelAnimation? animation)
    {
        ArgumentNullException.ThrowIfNull(meshes);
        ArgumentNullException.ThrowIfNull(worldTransforms);
        ArgumentOutOfRangeException.ThrowIfNotEqual(meshes.Length, worldTransforms.Length);

        Info = modelInfo;
        Bounds = bounds;
        Meshes = meshes;
        WorldTransforms = worldTransforms;
        Animation = animation;
    }

    public AnimationId AnimationId { get; private set; }

    public ModelInfo Info { get; }

    public BoundingBox Bounds { get; }

    public MeshEntry[] Meshes { get; }
    public Matrix4x4[] WorldTransforms { get; }

    public ModelAnimation? Animation { get; }

    //
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;

    public void AttachAnimation(AnimationId animationId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(animationId.Value, 0, nameof(animationId));
        InvalidOpThrower.ThrowIf(AnimationId.Value > 0, nameof(ModelId));
        InvalidOpThrower.ThrowIfNull(Animation);

        AnimationId = animationId;
    }

    internal override AssetObject CopyAndIncreaseGen() => throw new NotImplementedException();

}