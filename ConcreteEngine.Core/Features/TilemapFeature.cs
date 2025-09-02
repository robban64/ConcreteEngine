#region

using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Descriptors;

#endregion

namespace ConcreteEngine.Core.Features;

public sealed class TilemapDrawData
{
    public MaterialId MaterialId { get; set; }
    public int MapDimension { get; set; } = 64;
    public int TileSize { get; set; } = 32;
    public int Count { get; set; } = 0;
}

public class TilemapFeature : GameFeature, IDrawableFeature<TilemapDrawData>
{
    private readonly TilemapDrawData _drawData = new();

    public override bool IsUpdateable => true;
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    public override void Initialize()
    {
    }

    public override void UpdateTick(int tick)
    {
        var tilemaps = Context.World.Tilemaps;
        _drawData.Count = tilemaps.Count;
        if (tilemaps.Count > 0)
        {
            ref var node = ref tilemaps.ByIndex(0);

            _drawData.MapDimension = node.MapSize;
            _drawData.TileSize = node.TileSize;
            _drawData.MaterialId = node.MaterialId;
        }
    }

    public override void Update(in FrameMetaInfo frameCtx)
    {
    }


    public TilemapDrawData GetDrawables() => _drawData;
}