using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Render.Components;

namespace ConcreteEngine.Core.Engine.Render;


[StructLayout(LayoutKind.Sequential)]
public readonly struct DrawEntityKey(int entity, uint sortKey) : IRenderComponent<DrawEntityKey>
{
    public readonly int Entity = entity;
    public readonly uint SortKey = sortKey;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(DrawEntityKey e) => e.Entity;

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DrawEntityKey Create(int entity, PassMask passMask, float distance, DrawQueue queue)
    {
        uint clampedDistance = (uint)float.Clamp(distance, 0f, 65535f);
        uint depthKey = queue < DrawQueue.Transparent ? clampedDistance : clampedDistance ^ ushort.MaxValue;
        var sortKey = (byte)passMask | (depthKey << 8) | ((uint)queue << 24);
        return new DrawEntityKey(entity, sortKey);
    }
}

