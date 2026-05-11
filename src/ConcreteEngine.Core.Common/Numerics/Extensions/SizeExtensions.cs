using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Common.Numerics.Extensions;

public static class SizeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size2D ToSize2D(this Vector2D<int> v) => new(v.X, v.Y);
}