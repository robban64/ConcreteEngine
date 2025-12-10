#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Data;

#endregion

namespace ConcreteEngine.Engine.Worlds.Tables;

public readonly struct MeshPart(MeshId mesh, int materialSlot, int drawCount)
{
    public readonly MeshId Mesh = mesh;
    public readonly int MaterialSlot = materialSlot;
    public readonly int DrawCount = drawCount;
    private readonly int _pad; // ensure 16 byte
}

internal readonly ref struct ModelBoundsView(BoundingBox[] bounds)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteModelBoundingBox(ModelId id, ref BoundingBox result) => result = bounds[id - 1];

    public void FillModelBoundingBox(ModelId id, out BoundingBox result) => result = bounds[id - 1];
    // public ref readonly BoundingBox ModelBoundingBox(ModelId id) =>ref  bounds[id - 1];
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
    BoneTrack[][][] clips,
    Matrix4x4[] boneOffsetMatrix,
    Matrix4x4[] nodeTransform,
    int[] parentIndices,
    Matrix4x4[] modelBoneInvTransform)
{
    private const int BoneCap = RenderLimits.BoneCapacity;

    public ModelAnimationView GetModelView(AnimationId animation, out Matrix4x4 invTransform)
    {
        const int boneCap = RenderLimits.BoneCapacity;

        var index = animation - 1;
        var startOffset = index * boneCap;
        var endOffset = startOffset + boneCap;

        if ((uint)index > clips.Length || (uint)index > modelBoneInvTransform.Length ||
            (uint)endOffset > boneOffsetMatrix.Length || nodeTransform.Length != boneOffsetMatrix.Length)
        {
            throw new IndexOutOfRangeException();
        }


        var boneTransforms = boneOffsetMatrix.AsSpan(startOffset, boneCap);
        var nodes = nodeTransform.AsSpan(startOffset, boneCap);
        var indices = parentIndices.AsSpan(startOffset, boneCap);
        var clip = clips[index];
        invTransform = modelBoneInvTransform[index];
        return new ModelAnimationView(clip, boneTransforms, nodes, indices);
    }
}

internal readonly ref struct ModelAnimationView(
    BoneTrack[][] clips,
    ReadOnlySpan<Matrix4x4> boneOffsetMatrixSpan,
    ReadOnlySpan<Matrix4x4> nodeTransformSpan,
    ReadOnlySpan<int> parentIndexSpan)
{
    public readonly ReadOnlySpan<Matrix4x4> BoneOffsetMatrixSpan = boneOffsetMatrixSpan;
    public readonly ReadOnlySpan<Matrix4x4> NodeTransformSpan = nodeTransformSpan;
    public readonly ReadOnlySpan<int> ParentIndexSpan = parentIndexSpan;

    public int BoneLength => ParentIndexSpan.Length;

    public ReadOnlySpan<BoneTrack> GetClip(int clip) => clips[clip];
}