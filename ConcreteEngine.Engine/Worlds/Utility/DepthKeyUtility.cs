#region

using System.Numerics;
using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Engine.Worlds.Utility;

internal static class DepthKeyUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ExtractView(in Matrix4x4 v) => new(v.M13, v.M23, v.M33, v.M43);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort MakeDepthKey(
        in Vector4 view,
        in Vector3 worldPos,
        float near,
        float far)
    {
        var z = worldPos.X * view.X + worldPos.Y * view.Y + worldPos.Z * view.Z + view.W;
        var d = -z;

        if (d <= near) return 0;
        if (d >= far) return ushort.MaxValue;

        var t = (d - near) / (far - near);
        return (ushort)(t * ushort.MaxValue + 0.5f);
    }
}