using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Common.Numerics.Extensions;

public static partial class VectorExtensions
{
    public static Int2 ToVec2Int(this Vector2D<int> v) => new(v.X, v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Range(this Int2 v) => v.Y - v.X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Range(this Vector2 v) => v.Y - v.X;
}