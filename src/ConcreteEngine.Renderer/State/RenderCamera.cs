using System.Numerics;
using ConcreteEngine.Core.Renderer.Data;

namespace ConcreteEngine.Renderer.State;

public sealed class RenderCamera
{
    public ViewMatrixData RenderView;
    public ViewTransform Transform;
    public ViewMatrixData LightSpace;

    internal bool UseLightViewOverride;

    internal void RestoreView() => UseLightViewOverride = false;
    internal void ToggleLightView() => UseLightViewOverride = true;

    internal Vector3 Right
    {
        get
        {
            scoped ref readonly var view = ref RenderView.ViewMatrix;
            return new Vector3(view.M11, view.M21, view.M31);
        }
    }

    internal Vector3 Up
    {
        get
        {
            scoped ref readonly var view = ref RenderView.ViewMatrix;
            return new Vector3(view.M12, view.M22, view.M32);
        }
    }

    internal Vector3 Forward
    {
        get
        {
            scoped ref readonly var view = ref RenderView.ViewMatrix;
            return -new Vector3(view.M13, view.M23, view.M33);
        }
    }
}