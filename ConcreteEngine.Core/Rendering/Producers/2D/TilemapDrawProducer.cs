#region

using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Rendering.Data;

#endregion

namespace ConcreteEngine.Core.Rendering.Producers;

public struct TilemapDrawData()
{
    public MaterialId MaterialId;
    public int MapDimension = 64;
    public int TileSize = 32;
    public int Count = 0;
}
/*
public interface ITilemapDrawSink : IDrawSink
{
    void Send(TilemapDrawData payload);
}
public sealed class TilemapDrawProducer : IDrawCommandProducer, ITilemapDrawSink
{
    private static readonly Matrix4x4  TilemapTransform =
        TransformHelper.CreateTransform2D(Vector2.Zero, new Vector2(1, 1), 0);

    private TilemapBatcher _tilemapBatcher = null!;

    private CommandProducerContext _context = null!;

    private TilemapDrawData? _data = null;

    public void Send(TilemapDrawData payload)
    {
        _data = payload;
    }

    public void AttachContext(CommandProducerContext ctx)
    {
        _context = ctx;
    }

    public void Initialize()
    {
        _tilemapBatcher = _context.DrawBatchers.GetByRef<TilemapBatcher>();
    }

    public void BeginTick(in UpdateMetaInfo updateMeta)
    {
    }

    public void EndTick()
    {
    }

    public void EmitFrame(float alpha, IRenderPipeline submitter)
    {
        if(_data == null) return;
        var data = _data.Value;

        var result = _tilemapBatcher.BuildBatch();

        var cmd = new DrawCommandSprite(
            meshId: result.GroundLayer.MeshId,
            drawCount: result.GroundLayer.DrawCount,
            materialId: data.MaterialId,
            transform: in TilemapTransform
        );

        var meta = DrawCommandMeta.Make2D(DrawCommandId.Tilemap, RenderTargetId.Scene);
        submitter.SubmitDraw(in cmd, in meta);
    }
}*/