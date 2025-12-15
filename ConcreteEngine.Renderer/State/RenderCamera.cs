using System.Numerics;
using ConcreteEngine.Shared.World;

namespace ConcreteEngine.Renderer.State;

public sealed class RenderCamera
{
    public ViewMatrixData RenderView;
    public ViewTransform Transform;
    public LightView LightSpace;

    public bool UseLightViewOverride { get; internal set; }

    public void RestoreView() => UseLightViewOverride = false;
    public void ToggleLightView() => UseLightViewOverride = true;

    public Vector3 Right => new(RenderView.ViewMatrix.M11, RenderView.ViewMatrix.M21, RenderView.ViewMatrix.M31);
    public Vector3 Up => new Vector3(RenderView.ViewMatrix.M12, RenderView.ViewMatrix.M22, RenderView.ViewMatrix.M32);

    public Vector3 Forward =>
        -new Vector3(RenderView.ViewMatrix.M13, RenderView.ViewMatrix.M23, RenderView.ViewMatrix.M33);
}