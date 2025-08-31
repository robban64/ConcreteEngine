using System.Numerics;
using ConcreteEngine.Core;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Data;
using Silk.NET.Maths;

namespace Demo;

public class NpcSpriteModule : GameModule
{
    private readonly List<SpriteEntity> _spriteNodes = [];
    private readonly SpriteAtlas _spriteAtlas = new();

    private int _animationCountdown = 3;
    private int _currentFrame = 0;


    public override void Initialize()
    {
    }

    public override void OnSceneReady()
    {
        var spriteTexture = Context.GetSystem<IAssetSystem>().Get<Texture2D>("SpriteTexture");
        _spriteAtlas.Set(new Vector2D<int>(64, 64), new Vector2D<int>(spriteTexture.Width, spriteTexture.Height));

        var nodes = Context.World.Sprites.AsSpan();
        foreach (ref var node in nodes)
        {
            node.UvScale = _spriteAtlas.InvTexSizePx;
        }
        _spriteNodes.AddRange(nodes);
       
    }

    public override void UpdateTick(int tick)
    {
        const float speed = 2;

        _animationCountdown--;

        if (_animationCountdown == 0)
        {
            if (++_currentFrame % 9 == 0) _currentFrame = 0;
            _animationCountdown = 3;
        }

        foreach (ref var sprite in Context.World.Sprites.AsSpan())
        {
            sprite.SourceRectangle = _spriteAtlas.At(_currentFrame, 1);
        }
    }

    public override void Update(in FrameMetaInfo frameCtx)
    {
    }
}