#region

#endregion

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Core.Transforms;

namespace ConcreteEngine.Core;

public sealed class GameSceneContext
{
    public required FeatureManager Features { get; init; }
    public required ModuleManager Modules { get; init; }
    public required InputSystem InputSystem { get; init; }
    public required CameraSystem CameraSystem { get; init; }
    public required RenderSystem RenderSystem { get; init; }
    public required AssetSystem AssetSystem { get; init; }


    internal GameSceneContext()
    {
    }
}

public sealed class GameFeatureContext
{
    private readonly GameSceneContext _scene;

    public ModuleManager Modules => _scene.Modules;
    public InputSystem InputSystem => _scene.InputSystem;
    public CameraSystem CameraSystem => _scene.CameraSystem;
    public RenderSystem RenderSystem => _scene.RenderSystem;
    public AssetSystem AssetSystem => _scene.AssetSystem;


    internal GameFeatureContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}

public sealed class GameModuleContext
{
    private readonly GameSceneContext _scene;
    
    public CameraSystem CameraSystem => _scene.CameraSystem;
    public InputSystem InputSystem => _scene.InputSystem;
    public AssetSystem AssetSystem => _scene.AssetSystem;

    internal GameModuleContext(GameSceneContext scene)
    {
        _scene = scene;
    }

}