#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render.Data;

public readonly struct MeshPart(MeshId mesh, int materialSlot, int drawCount)
{
    public readonly MeshId Mesh = mesh;
    public readonly int MaterialSlot = materialSlot;
    public readonly int DrawCount = drawCount;
    private readonly int _pad; // ensure 16 byte
}

internal readonly ref struct ModelBoundsView(ReadOnlySpan<BoundingBox> bounds)
{
    private readonly ReadOnlySpan<BoundingBox> _bounds = bounds;
    public void WriteModelBoundingBox(ModelId id, out BoundingBox bounds) => bounds = _bounds[id - 1];
}

internal readonly ref struct ModelPartView(
    ReadOnlySpan<MeshPart> parts,
    ReadOnlySpan<Matrix4x4> locals,
    ReadOnlySpan<BoundingBox> bounds,
    RangeU16 ranges)
{
    public readonly ReadOnlySpan<MeshPart> Parts = parts;
    public readonly ReadOnlySpan<Matrix4x4> Locals = locals;
    public readonly ReadOnlySpan<BoundingBox> Bounds = bounds;
    public readonly RangeU16 Range = ranges;
}

internal readonly ref struct ModelAnimationView(
    BoneTrack[][] clips,
    ReadOnlySpan<Matrix4x4> boneTransforms,
    ReadOnlySpan<Matrix4x4> nodeTransforms,
    ReadOnlySpan<int> parentIndices,
    ref Matrix4x4 invTransform)
{
    public readonly ReadOnlySpan<Matrix4x4> BoneTransforms = boneTransforms;
    public readonly ReadOnlySpan<Matrix4x4> NodeTransforms  = nodeTransforms;
    public readonly ReadOnlySpan<int> ParentIndices = parentIndices;
    public readonly ref Matrix4x4 InvTransform = ref invTransform;
    
    public ReadOnlySpan<BoneTrack> GetClip(int clip) => clips[clip - 1];
}

internal readonly ref struct AnimationBonePayload(ReadOnlySpan<Matrix4x4> boneTransforms, ReadOnlySpan<RangeU16> ranges)
{
    public readonly ReadOnlySpan<Matrix4x4> BoneTransforms = boneTransforms;
    public readonly ReadOnlySpan<RangeU16> Range = ranges;
}