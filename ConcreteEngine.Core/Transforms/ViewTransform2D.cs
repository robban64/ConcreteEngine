#region

using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Transforms;

public sealed class ViewTransform2D
{
    public Vector2D<float> Position { get; set; } = Vector2D<float>.Zero;
    public float Rotation { get; set; } = 0f; // In radians
    public float Zoom { get; set; } = 1f; // >1: zoom in, <1: zoom out
    public Vector2D<int> ViewportSize { get; set; }

    public Matrix4X4<float> ProjectionViewMatrix => ViewMatrix * ProjectionMatrix;

    public Matrix4X4<float> ViewMatrix
    {
        get
        {
            var translate = Matrix4X4.CreateTranslation(new Vector3D<float>(-Position, 0));
            var rotate = Matrix4X4.CreateRotationZ(-Rotation);
            var scale = Matrix4X4.CreateScale(new Vector3D<float>(Zoom, Zoom, 1f)); // Inverse zoom

            return scale * rotate * translate;
        }
    }

    public Matrix4X4<float> ProjectionMatrix
    {
        get
        {
            return Matrix4X4.CreateOrthographicOffCenter(
                0, ViewportSize.X / Zoom,
                ViewportSize.Y / Zoom, 0,
                -1f, 1f
            );
            /*
                float halfW = (ViewportSize.X * 0.5f) / Zoom;
                float halfH = (ViewportSize.Y * 0.5f) / Zoom;
                return Matrix4X4.CreateOrthographicOffCenter(
                   -halfW, halfW,
                   -halfH, halfH,
                   -1f, 1f
               );
             */
        }
    }
}