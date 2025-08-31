#region

using System.Runtime.InteropServices;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Emitters;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Core.Features;

public sealed class SpriteFeatureDrawData
{
    public List<SpriteDrawEntity> Entities { get; set; } = null!;
    public List<SpriteDrawBatch> Batches { get; set; } = null!;

    public ReadOnlySpan<SpriteDrawEntity> GetBatch(int batch)
    {
        var batchData  = Batches[batch];
        var entities = CollectionsMarshal.AsSpan(Entities).Slice(batchData.Start, batchData.End);
        return entities;
    }
}


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
    
    public override void UpdateTick(int tick)
    {
        var spriteRegistry = Context.World.Sprites;
        if (spriteRegistry.Count == 0) return;
        
        _entities.Clear();
        _batches.Clear();

        var transforms = Context.World.Transforms2D;
        foreach (var entry in spriteRegistry.GetEnumerator())
        {
            ref var sprite = ref entry.Value;
            ref var transform = ref transforms.Get(entry.Entity);
            
            _entities.Add(new SpriteDrawEntity
            {
                Position = transform.Position,
                PreviousPosition = sprite.PreviousPosition,
                Scale = transform.Scale,
                Uv = UvRect.GetInsetUv(sprite.SourceRectangle, sprite.UvScale),
                MaterialId = sprite.MaterialId,
            });
        }
    }

    public SpriteFeatureDrawData GetDrawables()
    {
        if(_entities.Count > 0)   
            _drawData.Batches = [new SpriteDrawBatch(_entities[0].MaterialId,0, _entities.Count)];
        else 
            _drawData.Batches = [];
        
        _drawData.Entities = _entities;
        return _drawData;
    }
}