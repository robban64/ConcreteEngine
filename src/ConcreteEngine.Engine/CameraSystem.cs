using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine;

public sealed class CameraSystem
{
    internal static readonly CameraSystem Instance = new();

    public readonly CameraTransform Camera;
    public readonly RayCaster RayCaster;

    private CameraSystem()
    {
        Camera = new CameraTransform(EngineSettings.Instance.Display.WindowSize);
        RayCaster = new RayCaster(Camera);
    }

    internal void AttachRaycast(SceneManager sceneManager, EngineRenderSystem renderSystem) =>
        RayCaster.Attach(sceneManager,renderSystem);
}