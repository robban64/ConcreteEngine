using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;

namespace ConcreteEngine.Engine.Render;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct DrawEntityIndex(RenderEntityId entity, uint sortKey)
{
    public readonly RenderEntityId Entity = entity;
    public readonly uint SortKey = sortKey;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawEntityIndex Create(RenderEntityId entity, byte mask, ushort depthKey, DrawQueue queue)
    {
        var depth = queue < DrawQueue.Transparent ? depthKey : (ushort)(ushort.MaxValue - depthKey);
        var sortKey = mask | ((uint)depth << 8) | ((uint)queue << 24);
        return new DrawEntityIndex(entity, sortKey);
    }

}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntityTicket(RenderEntityId entity, int submitIndex)
{
    public RenderEntityId Entity = entity;
    public int SubmitIndex = submitIndex;
}
