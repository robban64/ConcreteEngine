#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Models;
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

public readonly ref struct ModelPartView(
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

public readonly ref struct ModelAnimationView(
    ModelAnimation animations,
    ReadOnlySpan<Matrix4x4> boneTransforms,
    ref Matrix4x4 invTransform,
    RangeU16 range)
{
    public readonly ModelAnimation Animations = animations;
    public readonly ReadOnlySpan<Matrix4x4> BoneTransforms = boneTransforms;
    public readonly ref Matrix4x4 InvTransform = ref invTransform;
    public readonly RangeU16 Range = range;
}

public readonly ref struct AnimationBonePayload(ReadOnlySpan<Matrix4x4> boneTransforms, ReadOnlySpan<RangeU16> ranges)
{
    public readonly ReadOnlySpan<Matrix4x4> BoneTransforms = boneTransforms;
    public readonly ReadOnlySpan<RangeU16> Range = ranges;
}