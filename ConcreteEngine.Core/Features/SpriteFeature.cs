#region

using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Utils;

#endregion

namespace ConcreteEngine.Core.Features;

public sealed class SpriteFeatureDrawData
{
    public int Count { get; set; }
    public SpriteDrawEntity[] Entities { get; set; } = null!;
    public List<(MaterialId, int)> Batches { get; set; } = [];
}

public class SpriteFeature : GameFeature, IDrawableFeature<SpriteFeatureDrawData>
{
    public override bool IsUpdateable => true;
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    private readonly List<(MaterialId, int)> _batches = [];
    private SpriteDrawEntity[] _entities = new SpriteDrawEntity[16];

    private int _entityIdx = 0;

    private readonly SpriteFeatureDrawData _drawData = new();

    public override void Initialize()
    {
    }

    public override void UpdateTick(int tick)
    {
        _entityIdx = 0;

        var spriteStore = Context.World.Sprites;
        if (spriteStore.Count == 0) return;

        var transforms = Context.World.Transforms2D;
        var prevTransforms = Context.World.PrevTransforms2D;

        if (_entities.Length < spriteStore.Count)
        {
            var newSize = int.Max(_entities.Length * 2, spriteStore.Count);
            Array.Resize(ref _entities, newSize);
        }

        foreach (var entry in spriteStore.View3(transforms, prevTransforms))
        {
            ref var sprite = ref entry.Value1;
            ref var transform = ref entry.Value2;
            ref var prevTransform = ref entry.Value3;

            _entities[_entityIdx++] = new SpriteDrawEntity
            {
                SpriteId = sprite.SpriteId,
                Position = transform.Position,
                PreviousPosition = prevTransform.Position,
                Scale = transform.Scale,
                Uv = UvRect.GetInsetUv(sprite.SourceRectangle, sprite.UvScale),
                MaterialId = sprite.MaterialId
            };
        }

        var entitiesSpan = _entities.AsSpan(0, _entityIdx);
        entitiesSpan.Sort();

        _batches.Clear();
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