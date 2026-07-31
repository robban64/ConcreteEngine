using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public struct EntityHeader
{
    public bool Visible;
    public EntityStatus Status;
}

public struct DrawPolicy(DrawQueue queue, PassMask passes)
{
    public DrawQueue Queue = queue;
    public PassMask Passes = passes;
}

public struct RenderSource(MeshId mesh, Id16<Material> material, int meshIndex = 0, DrawEntityFlags flags = 0)
{
    //public uint InstanceCount;
    public MeshId Mesh = mesh;
    public Id16<Material> Material = material;

    public byte MeshIndex = (byte)meshIndex;
    public DrawEntityFlags DrawFlags = flags;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsSkinned() => (DrawFlags & DrawEntityFlags.Skinned) != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsInstanced() => (DrawFlags & DrawEntityFlags.Instanced) != 0;

}

