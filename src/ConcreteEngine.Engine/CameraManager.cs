using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Scene;
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
    internal void Update(VisualManager visuals)
    {
        var lightDir = visuals.Illumination.DirectionalLight.Value.Direction;
        var shadow =  visuals.Shadow;
        var shadowProj = shadow.Projection;
        Camera.Ensure();
        Camera.UpdateLightView(Transforms, shadow.ShadowMapSize, shadowProj.Value.Distance, shadowProj.Value.ZPad, lightDir);
    }


    internal void AttachRaycast(SceneManager sceneManager, EngineRenderSystem renderSystem) =>
        RayCaster.Attach(sceneManager.Store, renderSystem);
}