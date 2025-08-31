#region

using System.Runtime.InteropServices;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Core.Features;

public sealed class SpriteFeatureDrawData
{
    public int Count { get; set; }
    public SpriteDrawEntity[] Entities { get; set; } = null!;
    public List<(MaterialId, int)> Batches { get; set; } = [];

    /*
    public ReadOnlySpan<SpriteDrawEntity> GetBatch(int batch)
    {
        var batchData  = Batches[batch];
        var entities = Entities.AsSpan(batchData.Start, batchData.End);
        return entities;
    }*/
}


public class SpriteFeature : GameFeature, IDrawableFeature<SpriteFeatureDrawData>
{
    public override bool IsUpdateable => true;
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    private readonly List<(MaterialId, int)> _batches = [];
    private SpriteDrawEntity[] _entities = new  SpriteDrawEntity[16];
    private int _entityIdx = 0;

    private readonly SpriteFeatureDrawData _drawData = new();

    private int _animationCountdown = 3;
    private int _dirCountdown = 20;
    private int _currentFrame = 0;

    public override void Initialize()
    {
    }
    
    public override void UpdateTick(int tick)
    {
        _entityIdx = 0;
        
        var spriteRegistry = Context.World.Sprites;
        if (spriteRegistry.Count == 0) return;
        _batches.Clear();

        if (_entities.Length < spriteRegistry.Count)
        {
            Array.Resize(ref _entities, spriteRegistry.Count);
        }

        var transforms = Context.World.Transforms2D;
        
        foreach (var entry in spriteRegistry.GetEnumerator())
        {
            ref var sprite = ref entry.Value;
            ref var transform = ref transforms.Get(entry.Entity);
            _entities[_entityIdx++] = new SpriteDrawEntity
            {
                Position = transform.Position,
                PreviousPosition = sprite.PreviousPosition,
                Scale = transform.Scale,
                Uv = UvRect.GetInsetUv(sprite.SourceRectangle, sprite.UvScale),
                MaterialId = sprite.MaterialId,
            };
        }

        var entitiesSpan = _entities.AsSpan(0,_entityIdx);
        entitiesSpan.Sort();
        
        
        var prevMaterialId = entitiesSpan[0].MaterialId;
        var curMaterialId = entitiesSpan[0].MaterialId;
        for (int i = 0; i < _entityIdx; i++)
        {
            ref var entity = ref entitiesSpan[i];
            curMaterialId = entity.MaterialId;
            if (curMaterialId != prevMaterialId)
            {
                _batches.Add((prevMaterialId, i));
            }
            prevMaterialId = curMaterialId;
        }
        _batches.Add((curMaterialId, _entityIdx));
        
        
/*
        int prevMaterialId = -1;
        foreach (var entry in spriteRegistry.GetEnumerator())
        {
            ref var sprite = ref entry.Value;
            ref var transform = ref transforms.Get(entry.Entity);
            if (prevMaterialId == -1 || prevMaterialId == sprite.MaterialId.Id)
            {
                if(!_batches.TryGetValue(sprite.MaterialId, out var batch))
                    _batches[sprite.MaterialId] = batch = (_entityIdx, _entityIdx);

                _batches[sprite.MaterialId] = (batch.Item1, _entityIdx + 1);
            }
            prevMaterialId =  sprite.MaterialId.Id;
            _entities[_entityIdx++] = new SpriteDrawEntity
            {
                Position = transform.Position,
                PreviousPosition = sprite.PreviousPosition,
                Scale = transform.Scale,
                Uv = UvRect.GetInsetUv(sprite.SourceRectangle, sprite.UvScale),
                MaterialId = sprite.MaterialId,
            };
        }*/
    }

    public SpriteFeatureDrawData GetDrawables()
    {
        if (_entityIdx == 0)
        {
            _drawData.Count = 0;
            return _drawData;
        }

        _drawData.Count = _entityIdx;
        _drawData.Entities = _entities;
        _drawData.Batches = _batches;
            
        return _drawData;
    }
}