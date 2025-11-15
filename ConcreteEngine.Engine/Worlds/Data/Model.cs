#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Engine.Worlds.Data;

public readonly record struct ModelId(int Value)
{
    public static implicit operator int(ModelId id) => id.Value;
}

public readonly struct MeshPart(MeshId mesh, int materialSlot, int drawCount)
{
    public readonly MeshId Mesh = mesh;
    public readonly int MaterialSlot = materialSlot;
    public readonly int DrawCount = drawCount;
    private readonly int _pad; // ensure 16 byte
}

public readonly ref struct ModelPartView(ReadOnlySpan<MeshPart> parts, ReadOnlySpan<Matrix4x4> locals, RangeU16 ranges)
{
    public readonly ReadOnlySpan<MeshPart> Parts = parts;
    public readonly ReadOnlySpan<Matrix4x4> Locals = locals;
    public readonly RangeU16 Range = ranges;
}