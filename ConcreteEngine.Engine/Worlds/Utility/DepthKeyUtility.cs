#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Engine.Worlds.Utility;

internal static class DepthKeyUtility
{
    public static ushort MakeDepthKey(
        in Matrix4x4 view,
        in Vector3 worldPos,
        float near,
        float far)
    {
        var z = worldPos.X * view.M13 + worldPos.Y * view.M23 + worldPos.Z * view.M33 + view.M43;
        var d = -z;

        if (d <= near) return 0;
        if (d >= far) return ushort.MaxValue;

        var t = (d - near) / (far - near);
        return (ushort)(t * ushort.MaxValue + 0.5f);
    }
}