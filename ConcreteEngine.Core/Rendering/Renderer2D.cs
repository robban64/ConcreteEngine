using ConcreteEngine.Core.Rendering.Batchers;

namespace ConcreteEngine.Core.Rendering;

public sealed class Renderer2D
{
    private readonly SpriteRenderer _spriteRenderer;
    private readonly LightRenderer _lightRenderer;

    private readonly SpriteBatcher _spriteBatch;
    private readonly TilemapBatcher _tilemapBatcher;

}