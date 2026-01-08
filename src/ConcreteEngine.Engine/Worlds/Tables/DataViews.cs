using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Worlds.Tables;

public readonly struct MeshPart(MeshId mesh, byte materialSlot, int drawCount)
{
    public readonly MeshId Mesh = mesh;

    public readonly byte MaterialSlot = materialSlot;
    //public readonly int DrawCount = drawCount;
    //private readonly int _pad; // ensure 16 byte
}

internal readonly ref struct ModelPartView(
    ReadOnlySpan<MeshPart> parts,
    ReadOnlySpan<Matrix4x4> locals,
    ReadOnlySpan<BoundingBox> bounds)
{
    public readonly ReadOnlySpan<MeshPart> Parts = parts;
    public readonly ReadOnlySpan<Matrix4x4> Locals = locals;
    public readonly ReadOnlySpan<BoundingBox> Bounds = bounds;
}

internal readonly ref struct AnimationDataView(
    Span<BoneTrack[][]> clips,
    Span<Matrix4x4> boneOffsetMatrix,
    Span<Matrix4x4> nodeTransform,
    Span<int> parentIndices,
    Span<Matrix4x4> modelBoneInvTransform)
{
    private readonly Span<Matrix4x4> _boneOffsetMatrix = boneOffsetMatrix;
    private readonly Span<Matrix4x4> _nodeTransform = nodeTransform;
    private readonly Span<int> _parentIndices = parentIndices;
    private readonly Span<Matrix4x4> _modelBoneInvTransform = modelBoneInvTransform;
    private readonly Span<BoneTrack[][]> _clips = clips;

    public ModelAnimationView GetModelView(AnimationId animation, out Matrix4x4 invTransform)
    {
        const int boneCap = RenderLimits.BoneCapacity;

        var index = animation.Index();
        var startOffset = index * boneCap;

        if (_nodeTransform.Length != _boneOffsetMatrix.Length || _parentIndices.Length != _nodeTransform.Length)
            throw new IndexOutOfRangeException();
        if ((uint)index >= _modelBoneInvTransform.Length)
            throw new IndexOutOfRangeException();


        var boneTransforms = _boneOffsetMatrix.Slice(startOffset, boneCap);
        var nodes = _nodeTransform.Slice(startOffset, boneCap);
        var indices = _parentIndices.Slice(startOffset, boneCap);
        invTransform = _modelBoneInvTransform[index];
        return new ModelAnimationView(_clips[index], boneTransforms, nodes, indices);
    }
}

internal readonly ref struct ModelAnimationView
{
    public readonly Span<Matrix4x4> BoneOffsetMatrix;
    public readonly Span<Matrix4x4> NodeTransform;
    public readonly Span<int> ParentIndex;
    private readonly Span<BoneTrack[]> _clips;

    public ModelAnimationView(
        Span<BoneTrack[]> clips,
        Span<Matrix4x4> boneOffsetMatrix,
        Span<Matrix4x4> nodeTransform,
        Span<int> parentIndex)
    {
        BoneOffsetMatrix = boneOffsetMatrix;
        NodeTransform = nodeTransform;
        ParentIndex = parentIndex;
        _clips = clips;
    }

    public int BoneLength => BoneOffsetMatrix.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<BoneTrack> GetClip(int clip) => _clips[clip];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TuplePtr<Matrix4x4, Matrix4x4> GetBoneDataPtr(int index, out int parent)
    {
        parent = ParentIndex[index];
        return new TuplePtr<Matrix4x4, Matrix4x4>(ref BoneOffsetMatrix[index], ref NodeTransform[index]);
    }
}