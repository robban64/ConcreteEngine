using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine;

public sealed class CameraManager
{
    internal static readonly CameraManager Instance = new();

    public readonly Camera Camera;
    public readonly RayCaster RayCaster;
    internal readonly CameraTransforms Transforms;

    private CameraManager()
    {
        Camera = new Camera(EngineSettings.Instance.Display.WindowSize);
        RayCaster = new RayCaster(Camera);
        Transforms = new CameraTransforms();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateFrameView(float alpha)
    {
        Camera.UpdateFrameView(Transforms, alpha);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Update(VisualEnvironment visualEnv)
    {
        visualEnv.Ensure();
        var lightDir = visualEnv.GetDirectionalLight().Direction;
        ref readonly var shadow = ref visualEnv.GetShadow();
        
        Camera.Ensure();
        Camera.UpdateLightView(Transforms, shadow.ShadowMapSize, shadow.Distance, shadow.ZPad, lightDir);
    }


    internal void AttachRaycast(SceneManager sceneManager, EngineRenderSystem renderSystem) =>
        RayCaster.Attach(sceneManager.Store, renderSystem);
}