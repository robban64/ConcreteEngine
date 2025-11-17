using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;

namespace ConcreteEngine.Engine.Worlds.View;

public sealed class CameraRaycaster
{
    private Matrix4x4 _invViewProjection;
    private Size2D _viewport;

    public Ray CreateRayFrom(Vector2 screenCoord)
    {
        var ndc = CoordinateMath.ToNdcCoords(screenCoord, _viewport);
        UnProject(new Vector3(ndc, -1.0f), in _invViewProjection, out var p1); // near
        UnProject(new Vector3(ndc, 1.0f), in _invViewProjection, out var p2); // far
        return Ray.FromTwoPoints(in p1, in p2);
    }

    internal void UpdateTick(Size2D viewport, in Matrix4x4 view, in Matrix4x4 projection)
    {
        _viewport = viewport;
        Matrix4x4.Invert(view, out var invView);
        Matrix4x4.Invert(projection, out var invProjection);
        _invViewProjection = invProjection * invView;
    }


    private static void UnProject(in Vector3 mouseNdc, in Matrix4x4 invViewProjection, out Vector3 point)
    {
        var vec = new Vector4(mouseNdc, 1.0f);
        vec = Vector4.Transform(vec, invViewProjection);

        if (vec.W > float.Epsilon || vec.W < -float.Epsilon)
        {
            vec.X /= vec.W;
            vec.Y /= vec.W;
            vec.Z /= vec.W;
        }

        point = new Vector3(vec.X, vec.Y, vec.Z);
    }
}