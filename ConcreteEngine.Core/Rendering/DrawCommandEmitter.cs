using ConcreteEngine.Core.Module;
using ConcreteEngine.Core.Rendering.Sprite;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;

namespace ConcreteEngine.Core.Rendering;

public interface IDrawCommandEmitter
{
    int Order { get; }
    void Initialize(IModuleRegistry registry);
    void Emit(DrawEmitterContext context, DrawCommandSubmitter submitter);
}

public sealed class DrawEmitterContext
{
    public IGraphicsDevice Graphics { get; init; }
    public SpriteBatchController SpriteBatch { get; init; }
}

public sealed class SpriteDrawCommandEmitter : IDrawCommandEmitter
{
    public int Order { get; }

    public SpriteModule SpriteModule { get; set; } = null!;

    public void Initialize(IModuleRegistry registry)
    {
        SpriteModule = registry.Get<SpriteModule>();
    }

    public void Emit(DrawEmitterContext context,  DrawCommandSubmitter submitter)
    {
        var spriteBatch = context.SpriteBatch;
        spriteBatch.BeginBatch("default", SpriteModule.SpriteTexture.ResourceId, SpriteModule.SpriteShader.ResourceId);
        var item = SpriteBatchDrawItem.From(SpriteModule.Transform,
            SpriteModule.SpriteAtlas.GetOffset(SpriteModule.column, SpriteModule.row), SpriteModule.SpriteAtlas.Scale);
        spriteBatch.SubmitSprite(item);
        var cmd = spriteBatch.FlushBatch();

        submitter.SubmitDraw(cmd, new DrawCommandMeta(RenderTargetId.None, 0));
    }
}