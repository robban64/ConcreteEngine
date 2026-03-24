using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine;

public sealed class CameraManager
{
    internal static readonly CameraManager Instance = new();

    public readonly Camera Camera;
    public readonly RayCaster RayCaster;
    internal readonly CameraRenderTransforms RenderTransforms;

    private CameraManager()
    {
        Camera = new Camera(EngineSettings.Instance.Display.WindowSize);
        RayCaster = new RayCaster(Camera);
        RenderTransforms = new CameraRenderTransforms();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateFrameView(float alpha)
    {
        Camera.UpdateFrameView(RenderTransforms, alpha);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateLightView(VisualEnvironment visualEnv)
    {
        visualEnv.Ensure();
        var lightDir = visualEnv.GetDirectionalLight().Direction;
        Camera.UpdateLightView(RenderTransforms, in visualEnv.GetShadow(), lightDir);
    }


    internal void AttachRaycast(SceneManager sceneManager, EngineRenderSystem renderSystem) =>
        RayCaster.Attach(sceneManager, renderSystem);
}