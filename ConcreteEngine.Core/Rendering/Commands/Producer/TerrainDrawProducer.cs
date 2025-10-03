#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Core.Rendering.Batching;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.Passes;

#endregion

namespace ConcreteEngine.Core.Rendering.Commands;

public struct TerrainDrawData
{
    public Texture2D? Heightmap;
    public MaterialId MaterialId;
    public int MaxHeight;
    public int Step;
}

public interface ITerrainDrawSink : IDrawSink
{
    void Send(TerrainDrawData payload);
}

public sealed class TerrainDrawProducer : IDrawCommandProducer, ITerrainDrawSink
{
    private TerrainBatcher _terrain = null!;

    private CommandProducerContext _context = null!;

    private TerrainDrawData? _data = null;

    public void Send(TerrainDrawData payload)
    {
        _data = payload;
    }

    public void AttachContext(CommandProducerContext ctx)
    {
        _context = ctx;
    }

    public void Initialize()
    {
        _terrain = _context.DrawBatchers.Get<TerrainBatcher>();
    }

    public void BeginTick(in UpdateInfo update)
    {
    }

    public void EndTick()
    {
    }


    public void EmitFrame(float alpha, in RenderGlobalSnapshot snapshot, DrawCommandBuffer submitter)
    {
        if (_data == null) return;

        var data = _data.Value;

        if (data.Heightmap != null && _terrain.HeightMap == null)
        {
            _terrain.Initialize(data.Heightmap, data.MaxHeight, data.Step);
            _terrain.BuildBatch();
        }

        if (_terrain.HeightMap == null) return;


        TransformUtils.CreateModelMatrix(new Vector3(-100, -10, -100), Vector3.One, Quaternion.Identity,
            out var transform);

        var cmd = new DrawCommand(
            meshId: _terrain.MeshId,
            drawCount: _terrain.DrawCount,
            materialId: data.MaterialId
        );

        var meta = new DrawCommandMeta(DrawCommandId.Terrain, RenderTargetId.Scene, DrawCommandQueue.Terrain);
        submitter.SubmitDraw(in cmd, in meta, new DrawTransformPayload(in transform));
    }
}