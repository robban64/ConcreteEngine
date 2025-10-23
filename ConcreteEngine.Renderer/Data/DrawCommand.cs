#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Renderer.Definitions;

#endregion

namespace ConcreteEngine.Renderer.Data;

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawCommand(MeshId meshId, MaterialId materialId, int drawCount = 0)
{
    public readonly MeshId MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly int DrawCount = drawCount;
}

//TODO Remove if not used in the future
[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawTransformPayload
{
    public readonly Matrix4x4 Transform;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawTransformPayload(in Matrix4x4 transform) => Transform = transform;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Fill(in Matrix4x4 model, out DrawTransformPayload dst)
        => dst = new DrawTransformPayload(in model);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawCommandMeta(
    DrawCommandId id,
    DrawCommandQueue queue,
    PassMask passMask = PassMask.Default,
    ushort depthKey = 0)
{
    public readonly PassMask PassMask = passMask;
    public readonly ushort DepthKey = depthKey;
    public readonly DrawCommandId Id = id;
    public readonly DrawCommandQueue Queue = queue;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct DrawCommandRef : IComparable<DrawCommandRef>
{
    private readonly ulong _sortKey;
    public readonly int Idx; // submit index, stable sort

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DrawCommandRef(DrawCommandMeta meta, int idx)
    {
        Idx = idx;
        _sortKey = ((ulong)meta.Queue << 48) |
                   ((ulong)meta.DepthKey << 32) |
                   (uint)idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandRef other) => _sortKey.CompareTo(other._sortKey);
}

internal readonly record struct DrawCommandTicket(int SubmitIdx /*, byte PassId*/);

internal readonly struct DrawPassRange(int start, int count)
{
    public readonly int Start = start;
    public readonly int Count = count;
}