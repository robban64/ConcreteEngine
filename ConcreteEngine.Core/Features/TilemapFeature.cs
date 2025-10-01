#region

#endregion

namespace ConcreteEngine.Core.Features;

public class TilemapFeature : GameFeature
{
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    //private ITilemapDrawSink _drawSink = null!;
    public override void Initialize()
    {
        //_drawSink = Context.GetSystem<IRenderSystem>().GetSink<ITilemapDrawSink>();
    }

    public override void UpdateTick(int tick)
    {
        /*
        var tilemaps = Context.World.Tilemaps;
        if (tilemaps.Count > 0)
        {
            ref var node = ref tilemaps.ByIndex(0);

            _drawSink.Send(new TilemapDrawData
            {
                Count = tilemaps.Count,
                MapDimension = node.MapSize,
                TileSize = node.TileSize,
            });
        }
        */
    }
}