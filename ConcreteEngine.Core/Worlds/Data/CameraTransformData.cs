using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Worlds.View;

namespace ConcreteEngine.Core.Worlds.Data;

internal readonly struct CameraTransformData(Vector3 translation, Vector3 scale, Quaternion rotation)
{
    public readonly Vector3 Translation = translation;
    public readonly Vector3 Scale = scale;
    public readonly Quaternion Rotation = rotation;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FromCamera(Camera3D camera, out CameraTransformData data)
    {
        data = new CameraTransformData(camera.Translation, camera.Scale, camera.Rotation);
    }
}