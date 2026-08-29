using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Identity;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.EcsRender;

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawPolicy(DrawQueue queue, PassMask passes, EntityDrawStatus status = EntityDrawStatus.Normal)
{
    public readonly EntityDrawStatus Status = status;
    public readonly PassMask Passes = passes;
    public readonly DrawQueue Queue = queue;
    private readonly byte _pad = 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawPolicy WithStatus(EntityDrawStatus status) => new(Queue, Passes, status);
}

public struct RenderSource(MeshId mesh, Id16<Material> material, int meshIndex = 0, EntityDrawFlags flags = 0)
{
    public MeshId Mesh = mesh;
    public Id16<Material> Material = material;

    public byte MeshIndex = (byte)meshIndex;
    public EntityDrawFlags DrawFlags = flags;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsSkinned() => (DrawFlags & EntityDrawFlags.Skinned) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsInstanced() => (DrawFlags & EntityDrawFlags.Instanced) != 0;
}