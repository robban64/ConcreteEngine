using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.EcsRender;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Engine.Render;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct DrawEntityIndex(RenderEntityIndex entity, uint sortKey)
{
    public readonly RenderEntityIndex Entity = entity;
    public readonly uint SortKey = sortKey;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawEntityIndex Create(RenderEntityIndex entity, byte mask, ushort depthKey, DrawQueue queue)
    {
        var depth = queue < DrawQueue.Transparent ? depthKey : (ushort)(ushort.MaxValue - depthKey);
        var sortKey = mask | ((uint)depth << 8) | ((uint)queue << 24);
        return new DrawEntityIndex(entity, sortKey);
    }

}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntityTicket(RenderEntityIndex entity, int submitIndex)
{
    public RenderEntityIndex Entity = entity;
    public int SubmitIndex = submitIndex;
}
