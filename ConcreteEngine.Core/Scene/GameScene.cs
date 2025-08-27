#region

using ConcreteEngine.Core.Configuration;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Core.Scene;

public abstract class GameScene
{
    protected GameSceneContext Context { get; private set; } = null!;
    
    protected GameScene()
    {
    }
    
    internal void Update(in FrameMetaInfo frameInfo)
    {
        Context.Features.Update(in frameInfo);
        Context.Modules.Update(in frameInfo);

    }

    internal void UpdateTick(int tick)
    {
        Context.Features.GameTickUpdate(tick);
        Context.Modules.GameTickUpdate(tick);
    }
    
    
    internal void AttachContext(GameSceneContext context) => Context = context;

    internal void Build(GameSceneConfigBuilder builder)
    {
        ConfigureRenderer(builder, builder.GraphicsDevice);
        ConfigureFeatures(builder);
        ConfigureModules(builder);
    }

    internal void InitializeInternal()
    {
        Initialize();
    }

    protected abstract void ConfigureRenderer(IGameSceneRenderBuilder builder, IGraphicsDevice graphics);
    protected abstract void ConfigureFeatures(IGameSceneFeatureBuilder builder);
    protected abstract void ConfigureModules(IGameSceneModuleBuilder builder);

    public abstract void Initialize();
    public abstract void Unload();
    

}