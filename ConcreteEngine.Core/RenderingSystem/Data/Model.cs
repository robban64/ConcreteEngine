using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Resources;

namespace ConcreteEngine.Core.RenderingSystem.Data;

public readonly record struct ModelId(int Value)
{
    public static implicit operator int(ModelId id) => id.Value;
}

public readonly struct MeshPart(MeshId mesh, int drawCount)
{
    public readonly MeshId Mesh = mesh;
    public readonly int DrawCount = drawCount;
}

public readonly ref struct ModelPartView(ReadOnlySpan<MeshPart> parts, ReadOnlySpan<Matrix4x4> locals, RangeU16 ranges)
{
    public readonly ReadOnlySpan<MeshPart> Parts = parts;
    public readonly ReadOnlySpan<Matrix4x4> Locals = locals;
    public readonly RangeU16 Range = ranges;
}