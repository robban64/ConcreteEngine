using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Renderer.Data;

public struct ViewTransform(in Vector3 translation, YawPitch orientation)
{
    public Vector3 Translation = translation;
    public YawPitch Orientation = orientation;
}