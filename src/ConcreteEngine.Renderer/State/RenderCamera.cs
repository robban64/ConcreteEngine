using System.Numerics;
using ConcreteEngine.Core.Specs.World;

namespace ConcreteEngine.Renderer.State;

public sealed class RenderCamera
{
    public ViewMatrixData RenderView;
    public ViewTransform Transform;
    public LightView LightSpace;

    internal bool UseLightViewOverride;

    internal void RestoreView() => UseLightViewOverride = false;
    internal void ToggleLightView() => UseLightViewOverride = true;

    internal Vector3 Right => new(RenderView.ViewMatrix.M11, RenderView.ViewMatrix.M21, RenderView.ViewMatrix.M31);
    internal Vector3 Up => new Vector3(RenderView.ViewMatrix.M12, RenderView.ViewMatrix.M22, RenderView.ViewMatrix.M32);

    internal Vector3 Forward =>
        -new Vector3(RenderView.ViewMatrix.M13, RenderView.ViewMatrix.M23, RenderView.ViewMatrix.M33);
}