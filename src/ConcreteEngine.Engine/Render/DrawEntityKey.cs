using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Engine.Render;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct DrawEntityKey(int entity, uint sortKey)
{
    public readonly int Entity = entity;
    public readonly uint SortKey = sortKey;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawEntityKey Create(int entity, PassMask mask, ushort depthKey, DrawQueue queue)
    {
        var depth = queue < DrawQueue.Transparent ? depthKey : (ushort)(ushort.MaxValue - depthKey);
        var sortKey = (byte)mask | ((uint)depth << 8) | ((uint)queue << 24);
        return new DrawEntityKey(entity, sortKey);
    }

}

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntityIndex(int entity, int submitIndex)
{
    public int Entity = entity;
    public int SubmitIndex = submitIndex;
}
