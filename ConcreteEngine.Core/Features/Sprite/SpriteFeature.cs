#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Data;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Features.Sprite;

public class SpriteFeature : GameFeature, IDrawableFeature<SpriteFeatureDrawData>
{
    public override bool IsUpdateable => true;
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    public Shader SpriteShader { get; set; } = null!;
    public Texture2D SpriteTexture { get; set; } = null!;

    private readonly List<(int, int)> _batches = [];
    private readonly List<SpriteDrawEntity> _entities = [];

    private readonly SpriteFeatureDrawData _drawData = new();
    
    private int _animationCountdown = 3;
    private int _dirCountdown = 20;
    private int _currentFrame = 0;

    public override void Initialize()
    {
        var assets = Context.GetSystem<IAssetSystem>();
        var renderer = Context.GetSystem<IRenderSystem>();
        
        SpriteShader = assets.Get<Shader>("SpriteShader");
        SpriteTexture = assets.Get<Texture2D>("SpriteTexture");
        
        renderer.SpriteBatch.CreateSpriteBatch(0, 64);

    }

    public override void CollectFrame(ISceneNodeCollector collector)
    {
        var spriteNodes = collector.GetSceneNodes<SpriteBehaviour>();
        if(spriteNodes.Count == 0) return;
        _entities.Clear();
        _batches.Clear();

        foreach (var spriteNode in spriteNodes)
        {
            var behaviour = (SpriteBehaviour)spriteNode.Behaviour;
            _entities.Add(new SpriteDrawEntity
            {
                Position = spriteNode.LocalTransform.Position,
                PreviousPosition = behaviour.PreviousPosition,
                Scale = spriteNode.LocalTransform.Scale,
                Uv = behaviour.GetUvRect()
            });
        }
    }

    public override void UpdateTick(int tick)
    {
    }

    public override void Update(in FrameMetaInfo frameCtx)
    {
    }
    
    public SpriteFeatureDrawData GetDrawables()
    {
        _drawData.Batches = [(0, _entities.Count)];
        _drawData.Entities = _entities;
        return _drawData;
    }

/*    private void CreateBatch(SpriteDrawEntity[] batch, int start, Vector2 offsetPosition)
    {
        int i = 0;
        for (int x = 0; x < 30; x++)
        {
            for (int y = 0; y < 30; y++)
            {
                batch[i+start] = (new SpriteDrawEntity
                {
                    Position = new Vector2(64 * x, 64 * y) + offsetPosition * 64 * 4,
                    Scale = new Vector2(64, 64),
                    AtlasLocation = new Vector2D<int>(0, 3),
                    Direction = new Vector2(-1, 0),
                    Uv = SpriteAtlas.GetUvRect(0, 3)
                });
                i++;
            }
        }
    }
    */
}