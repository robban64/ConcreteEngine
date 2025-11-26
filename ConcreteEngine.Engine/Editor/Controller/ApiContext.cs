#region

using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Renderer.State;

#endregion

namespace ConcreteEngine.Engine.Editor.Controller;

internal sealed class ApiContext
{
    public World World { get; }
    public AssetSystem AssetSystem { get; }

    private RenderFrameInfo _frameInfo;

    public ref readonly RenderFrameInfo FrameInfo => ref _frameInfo;

    public ApiContext(World world, AssetSystem assetSystem)
    {
        World = world;
        AssetSystem = assetSystem;
    }

    public void OnRenderFrame(in RenderFrameInfo renderFrameInfo)
    {
        _frameInfo = renderFrameInfo;
    }
}