#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Core.Systems;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Data;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Features;

public sealed class SpriteFeatureDrawData
{
    public int Count { get; set; }
    public SpriteDrawEntity[] Entities { get; set; } = null!;
    public List<(MaterialId, int)> Batches { get; set; } = [];
}

//TODO QuadTree
public class SpriteFeature : GameFeature, IDrawableFeature<SpriteFeatureDrawData>
{
    public override bool IsUpdateable => true;
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    private readonly List<(MaterialId, int)> _batches = [];
    private SpriteDrawEntity[] _entities = new  SpriteDrawEntity[16];
    private int _entityIdx = 0;

    private readonly SpriteFeatureDrawData _drawData = new();

    
    private ICameraSystem  _cameraSystem;

    public override void Initialize()
    {
        _cameraSystem = Context.GetSystem<ICameraSystem>();
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

        var camera = _cameraSystem.Camera.Transform;
        var transforms = Context.World.Transforms2D;
        
        foreach (var entry in spriteRegistry)
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