using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Diagnostic;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.Worlds;
using ConcreteEngine.Core.Worlds.Render;

namespace ConcreteEngine.Core.Scene;

internal sealed class SceneEditor
{
    private readonly World _world;
    private readonly WorldRenderer _worldRenderer;
    private readonly EngineGateway _engineGateway;

    private readonly AssetSystem _assetSystem;
    private readonly IEngineInputSource _input;

    private GameScene _scene;

    public SceneEditor(World world, WorldRenderer worldRenderer, EngineGateway engineGateway, AssetSystem assetSystem,
        IEngineInputSource input)
    {
        _world = world;
        _worldRenderer = worldRenderer;
        _engineGateway = engineGateway;
        _assetSystem = assetSystem;
        _input = input;
    }

    public void AttachScene()
    {

    }
}