using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Core.Engine.Assets;

public sealed class MeshEntry(string name, MeshInfo info)
{
    //[Inspectable(FieldKind = InspectorFieldKind.Name)]
    public readonly string Name = name;

    //[InspectablePrimitive(FieldKind = InspectorFieldKind.Id)]
    public MeshId MeshId;

    //[Inspectable] 
    public MeshInfo Info = info;

    public BoundingBox LocalBounds;
}

public sealed class Model : AssetObject
{
    public readonly ModelInfo Info;
    
    public readonly BoundingBox Bounds;

    public MeshEntry[] Meshes { get; }

    public ModelAnimation? Animation { get; }

    public Matrix4x4[] WorldTransforms { get; }

    public AnimationId AnimationId { get; private set; }
    
    //
    public override AssetKind Kind => AssetKind.Model;
    public override AssetCategory Category => AssetCategory.Graphic;


    public Model(string name,ModelInfo modelInfo, in BoundingBox bounds, MeshEntry[] meshes, Matrix4x4[] worldTransforms,
        ModelAnimation? animation): base(name)
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


    public void AttachAnimation(AnimationId animationId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(animationId.Value, 0, nameof(animationId));
        InvalidOpThrower.ThrowIf(AnimationId.Value > 0, nameof(ModelId));
        InvalidOpThrower.ThrowIfNull(Animation);

        AnimationId = animationId;
    }

    internal override AssetObject CopyAndIncreaseGen() => throw new NotImplementedException();
}