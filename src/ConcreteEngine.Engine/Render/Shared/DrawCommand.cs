using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Engine.Render;

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawCommandIndex : IComparable<DrawCommandIndex>
{
    private readonly ulong _sortKey;
    
    // submit index, stable sort
    public int Index => (int)((_sortKey >> 8) & 0x00FF_FFFF);
    public PassMask Pass => (PassMask)_sortKey;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawCommandIndex(int index, PassMask pass, DrawQueue queue, ushort depthKey)
    {
        var depth = queue < DrawQueue.Transparent ? depthKey : (ushort)(ushort.MaxValue - depthKey);
        _sortKey = ((ulong)queue << 48) |
                   ((ulong)depth << 32) |
                   ((ulong)index << 8) |
                   (byte)pass;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandIndex other) => _sortKey.CompareTo(other._sortKey);
}