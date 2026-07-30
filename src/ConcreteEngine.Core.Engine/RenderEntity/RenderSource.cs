using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.RenderEntity;

public struct DrawPolicy(DrawQueue queue, PassMask passes)
{
    public DrawQueue Queue = queue;
    public PassMask Passes = passes;
}

public struct RenderSource(
    MeshId mesh,
    Id16<Material> material,
    int meshIndex,
    EntitySourceKind kind)
{
    public uint InstanceCount;
    public MeshId Mesh = mesh;
    public Id16<Material> Material = material;

    public byte MeshIndex = (byte)meshIndex;
    public EntitySourceKind Kind = kind;
}

[StructLayout(LayoutKind.Sequential)]
public struct RenderEntityMeta
{
    public bool Alive;
    public EntityVisibility Visibility;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsVisible() => Alive && Visibility == 0;
    
}
