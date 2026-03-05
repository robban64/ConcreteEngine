using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine;

public sealed class CameraSystem
{
    internal static readonly CameraSystem Instance = new(EngineSettings.Instance.Display.WindowSize);

    public readonly CameraTransform Camera;
    public readonly RayCaster RayCaster;

    private CameraSystem(Size2D viewport)
    {
        Camera = new CameraTransform(viewport);
        RayCaster = new RayCaster(Camera);
    }

    internal void AttachRaycast(Terrain terrain, FrameEntityBuffer frameBuffer) =>
        RayCaster.Attach(terrain, frameBuffer);
}