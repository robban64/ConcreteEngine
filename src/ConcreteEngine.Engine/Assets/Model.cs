using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Editor;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets;

public sealed class MeshEntry
{
    [Inspectable(FieldKind = InspectorFieldKind.Name)] public readonly string Name;

    [InspectablePrimitive(FieldKind = InspectorFieldKind.Id)] public MeshId MeshId;

    [Inspectable] public MeshInfo Info;

    public BoundingBox LocalBounds;

    internal MeshEntry(string name, MeshInfo info)
    {
        Name = name;
        Info = info;
    }
}

public sealed class Model : AssetObject, IModel
{
    [Inspectable] public ModelInfo Info { get; }
    [Inspectable] public MeshEntry[] Meshes { get; }
    [Inspectable] public ModelAnimation? Animation { get; }
    
    public BoundingBox Bounds { get; }
    public Matrix4x4[] WorldTransforms { get; }

    public AnimationId AnimationId { get; private set; }

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