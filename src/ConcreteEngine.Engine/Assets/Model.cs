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

public sealed record Model : AssetObject, IModel
{
    public Model(int vertexCount, int faceCount, in BoundingBox bounds, MeshEntry[] meshes, Matrix4x4[] worldTransforms,
        ModelAnimation? animation)
    {
        VertexCount = vertexCount;
        FaceCount = faceCount;
        Bounds = bounds;
        Meshes = meshes;
        WorldTransforms = worldTransforms;
        Animation = animation;
    }

    private static ModelId CreateModelId() => new(++_modelIdx);
    private static int _modelIdx;

    public ModelId ModelId { get; } = CreateModelId();
    public AnimationId AnimationId { get; private set; }

    public int VertexCount { get; }
    public int FaceCount { get; }

    public BoundingBox Bounds { get; }

    public MeshEntry[] Meshes { get; }
    public Matrix4x4[] WorldTransforms { get; }

    public ModelAnimation? Animation { get; }

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