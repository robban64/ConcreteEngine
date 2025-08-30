#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Core.Features.Sprite;

public class SpriteFeature : GameFeature, IDrawableFeature<SpriteFeatureDrawData>
{
    public override bool IsUpdateable => true;
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    private readonly List<(int, int)> _batches = [];
    private readonly List<SpriteDrawEntity> _entities = [];

    private readonly SpriteFeatureDrawData _drawData = new();

    private int _animationCountdown = 3;
    private int _dirCountdown = 20;
    private int _currentFrame = 0;

    public override void Initialize()
    {
        var renderer = Context.GetSystem<IRenderSystem>();
        renderer.SpriteBatch.CreateSpriteBatch(0, 64);
    }

    public override void CollectFrame(ISceneNodeCollector collector)
    {
        var spriteNodes = collector.GetSceneNodes<SpriteBehaviour>();
        if (spriteNodes.Count == 0) return;
        _entities.Clear();
        _batches.Clear();

        foreach (var node in spriteNodes)
        {
            var behaviour = (SpriteBehaviour)node.Behaviour;
            _entities.Add(new SpriteDrawEntity
            {
                Position = node.LocalTransform.Position,
                PreviousPosition = behaviour.PreviousPosition,
                Scale = node.LocalTransform.Scale,
                Uv = behaviour.GetUvRect(),
                MaterialId = behaviour.MaterialId,
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
        if(_entities.Count > 0)   
            _drawData.Batches = [new SpriteDrawBatchData(_entities[0].MaterialId,0, _entities.Count)];
        else 
            _drawData.Batches = [];
        
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