#region

using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Scene.Nodes;
using ConcreteEngine.Graphics.Data;

#endregion

namespace ConcreteEngine.Core.Features.Terrain;

public class TilemapFeature : GameFeature, IDrawableFeature<TilemapDrawData>
{
    private TilemapDrawData _drawData = new();

    public override bool IsUpdateable => true;
    public bool IsDrawable { get; set; } = true;
    public int DrawOrder { get; set; } = 0;

    public override void Initialize()
    {
    }

    public override void CollectFrame(ISceneNodeCollector collector)
    {
        var tilemaps = collector.GetSceneNodes<TilemapBehaviour>();
        _drawData.Count = tilemaps.Count;
        if (tilemaps.Count > 0)
        {
            var node = tilemaps[0];
            var behaviour = (TilemapBehaviour)node.Behaviour;

            _drawData.MapDimension = behaviour.MapSize;
            _drawData.TileSize = behaviour.TileSize;
            _drawData.MaterialId = behaviour.MaterialId;
        }
    }

    public override void UpdateTick(int tick)
    {
    }

    public override void Update(in FrameMetaInfo frameCtx)
    {
    }


    public TilemapDrawData GetDrawables() => _drawData;
}