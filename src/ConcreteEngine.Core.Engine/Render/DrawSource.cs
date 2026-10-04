using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Render;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct DrawPolicy(DrawQueue Queue, PassMask Passes, EntityCullStatus Cull = EntityCullStatus.Normal)
{
    public readonly EntityCullStatus Cull = Cull;
    public readonly PassMask Passes = Passes;
    public readonly DrawQueue Queue = Queue;
    private readonly byte _pad;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawPolicy WithStatus(EntityCullStatus status) => new(Queue, Passes, status);
}

[StructLayout(LayoutKind.Sequential)]
public record struct DrawSource(MeshId Mesh, Id16<Material> Material, byte MeshIndex = 0, EntityDrawMask DrawMask = 0)
{
    public MeshId Mesh = Mesh;
    public Id16<Material> Material = Material;

    public byte MeshIndex = MeshIndex;
    public EntityDrawMask DrawMask = DrawMask;

    private ushort _pad;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsSkinned() => (DrawMask & EntityDrawMask.Skinned) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsInstanced() => (DrawMask & EntityDrawMask.Instanced) != 0;
}