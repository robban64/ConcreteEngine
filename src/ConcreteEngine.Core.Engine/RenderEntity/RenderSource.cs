using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public readonly struct DrawPolicy
{
    public readonly EntityDrawStatus Status;
    public readonly PassMask Passes;
    public readonly DrawQueue Queue;

    public DrawPolicy(DrawQueue queue, PassMask passes, EntityDrawStatus status = EntityDrawStatus.Normal)
    {
        Status = status;
        Queue = queue;
        Passes = passes;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly DrawPolicy WithStatus(EntityDrawStatus status)
    {
        return new DrawPolicy(Queue, Passes, status);
    }

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