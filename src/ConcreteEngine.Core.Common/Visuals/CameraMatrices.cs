using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Visuals;

public struct CameraMatrices(
    in Matrix4x4 viewMatrix,
    in Matrix4x4 projectionMatrix
)
{
    public static CameraMatrices CreateIdentity() => new(Matrix4x4.Identity, Matrix4x4.Identity);

    public Matrix4x4 ViewMatrix = viewMatrix;
    public Matrix4x4 ProjectionMatrix = projectionMatrix;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void GetProjectionView(out Matrix4x4 viewProj) => viewProj = ViewMatrix * ProjectionMatrix;
    
    public readonly Vector3 Right => new(ViewMatrix.M11, ViewMatrix.M21, ViewMatrix.M31);
    public readonly Vector3 Up => new(ViewMatrix.M12, ViewMatrix.M22, ViewMatrix.M32);
    public readonly Vector3 Forward => -new Vector3(ViewMatrix.M13, ViewMatrix.M23, ViewMatrix.M33);
}