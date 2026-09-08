using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.ECS.Render;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Engine.Render;

[StructLayout(LayoutKind.Sequential)]
internal struct DrawEntityKey(int entity, uint sortKey)
{
    public int Entity = entity;
    public uint SortKey = sortKey;

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawEntityKey Create(int entity, PassMask passMask, ushort distance, DrawQueue queue)
    {
        if (queue >= DrawQueue.Transparent) distance ^= ushort.MaxValue;
        DrawEntityKey result;
        result.Entity = entity;
        result.SortKey = (byte)passMask | ((uint)distance << 8) | ((uint)queue << 24);
        return result;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct DrawEntityIndex(int entity, int submitIndex)
{
    public readonly int Entity = entity;
    public readonly int SubmitIndex = submitIndex;
}