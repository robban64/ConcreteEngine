using System.Numerics;
using ConcreteEngine.Core;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Data;
using Silk.NET.Maths;

namespace Demo;

public class NpcSpriteModule : GameModule
{
    private readonly List<(SceneNode, SpriteBehaviour)> _spriteNodes = [];
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

        var nodes = Context.Nodes;
        _spriteNodes.Add(nodes.GetNodeWithBehaviour<SpriteBehaviour>("node1"));
        _spriteNodes.Add(nodes.GetNodeWithBehaviour<SpriteBehaviour>("node2"));

        _spriteNodes[0].Item2.UvScale = _spriteAtlas.InvTexSizePx;
        _spriteNodes[1].Item2.UvScale = _spriteAtlas.InvTexSizePx;
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

        foreach (var (node, behaviour) in _spriteNodes)
        {
            behaviour.PreviousPosition = node.LocalTransform.Position;

            node.LocalTransform.Position += new Vector2(0.4f, 0);
            behaviour.SourceRectangle = _spriteAtlas.At(_currentFrame, 1);
        }
    }

    public override void Update(in FrameMetaInfo frameCtx)
    {
    }
}