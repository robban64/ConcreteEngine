#region

using System.Numerics;
using ConcreteEngine.Core.Features;

#endregion

namespace ConcreteEngine.Core.Rendering;

public sealed class TilemapDrawProducer : DrawCommandProducer<TilemapDrawData>
{
    private static readonly Matrix4x4  TilemapTransform = 
        TransformHelper.CreateTransform2D(Vector2.Zero, new Vector2(1, 1), 0);
    
    private TilemapBatcher _tilemapBatcher = null!;
    
    public override void OnInitialize()
    {
        _tilemapBatcher = Context.DrawBatchers.Get<TilemapBatcher>();

    }

    protected override void EmitCommands(float alpha, TilemapDrawData data, DrawCommandSubmitter submitter)
    {
        if(data.Count == 0) return;
        
        var result = _tilemapBatcher.BuildBatch();
        
        var cmd = new DrawCommandSprite(
            meshId: result.GroundLayer.MeshId,
            drawCount: result.GroundLayer.DrawCount,
            materialId: data.MaterialId,
            transform: in TilemapTransform
        );

        var meta = new DrawCommandMeta(DrawCommandId.Tilemap, DrawCommandTag.Mesh2D, RenderTargetId.Scene, 0);
        submitter.SubmitDraw(in cmd, in meta);
    }
}