using System.Numerics;

namespace ConcreteEngine.Core.World;

internal readonly struct CameraTransformInfo(Vector3 translation, Vector3 scale, Quaternion rotation)
{
    public readonly Vector3 Translation = translation;
    public readonly Vector3 Scale = scale;
    public readonly Quaternion Rotation = rotation;

    public static void FromCamera(Camera3D camera, out CameraTransformInfo info)
    {
        info = new CameraTransformInfo(camera.Translation, camera.Scale, camera.Rotation);
    }
}