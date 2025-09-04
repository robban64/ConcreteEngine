using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public sealed class TerrainDrawData
{
    public Texture2D? Heightmap { get; set; }
    public MaterialId MaterialId { get; set; }
    public int MaxHeight { get; set; }
}


public sealed class TerrainDrawProducer : DrawCommandProducer<TerrainDrawData>
{
    private TerrainBatcher _terrain = null!;
    
    public override void OnInitialize()
    {
        _terrain = Context.DrawBatchers.Get<TerrainBatcher>();
    }

    protected override void EmitCommands(float alpha, TerrainDrawData data, DrawCommandSubmitter submitter)
    {
        if (data.Heightmap != null && _terrain.HeightMap == null)
        {
            _terrain.Initialize(data.Heightmap, 8, 1);
            _terrain.BuildBatch();
        }
        
        if(_terrain.HeightMap == null) return;
        
        
        var transform = TransformHelper
            .CreateTransform(new Vector3(-100, -10, -100), Vector3.One, Quaternion.Identity);
        
        var cmd = new DrawCommandTerrain(
            meshId: _terrain.MeshId,
            drawCount: _terrain.DrawCount,
            materialId: data.MaterialId,
            transform: in transform
        );
        

        var meta = new DrawCommandMeta(
            DrawCommandId.Terrain, DrawCommandTag.Terrain, RenderTargetId.Scene, DrawCommandQueue.OpaqueTerrain);
        submitter.SubmitDraw(in cmd, in meta);

    }
}