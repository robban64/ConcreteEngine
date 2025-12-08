#region

using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Shared.Graphics;

public struct UvRect(float U0, float V0, float U1, float V1)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UvRect GetInsetUv(Rectangle<int> sourcePxRect, Vector2 invTexSize)
    {
        // UV space
        var insetU = 0.5f * invTexSize.X;
        var insetV = 0.5f * invTexSize.Y;

        // Pixel to UV
        var u0 = sourcePxRect.Origin.X * invTexSize.X + insetU;
        var v0 = sourcePxRect.Origin.Y * invTexSize.Y + insetV;
        var u1 = (sourcePxRect.Origin.X + sourcePxRect.Size.X) * invTexSize.X - insetU;
        var v1 = (sourcePxRect.Origin.Y + sourcePxRect.Size.Y) * invTexSize.Y - insetV;

        return new UvRect(u0, v0, u1, v1);
    }
}