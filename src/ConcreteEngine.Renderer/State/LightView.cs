using System.Numerics;

namespace ConcreteEngine.Renderer.State;

public struct LightView
{
    public Matrix4x4 LightViewMatrix;
    public Matrix4x4 LightProjectionMatrix;
    public Matrix4x4 LightSpaceMatrix;

    //public Vector3 LightPosition;
    //public Vector3 LightDirection;

    public readonly Vector3 Right => new(LightViewMatrix.M11, LightViewMatrix.M21, LightViewMatrix.M31);
    public readonly Vector3 Up => new (LightViewMatrix.M12, LightViewMatrix.M22, LightViewMatrix.M32);
    public readonly Vector3 Forward => -new Vector3(LightViewMatrix.M13, LightViewMatrix.M23, LightViewMatrix.M33);
}