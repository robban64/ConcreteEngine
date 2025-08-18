#region

using System.Numerics;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Materials;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Game.Terrain;

public sealed class TilemapDrawEmitter : IDrawCommandEmitter
{
    public int Order { get; set; }

    public TilemapFeature Tilemap { get; set; } = null!;

    public void Initialize(IFeatureRegistry registry)
    {
        Tilemap = registry.Get<TilemapFeature>();
    }

    public void Emit(DrawEmitterContext context, DrawCommandSubmitter submitter)
    {
        var transform = Transform2D.CreateTransformMatrix(Vector2.Zero, new Vector2(1, 1), 0);

        var tilemapBatcher = context.TilemapBatch;
        var result = tilemapBatcher.BuildBatch();
        var cmd = new DrawCommandData(
            meshId: result.GroundLayer.MeshId,
            drawCount: result.GroundLayer.DrawCount,
            materialId: MaterialId.Of(1),
            transform: in transform
        );

        var meta = new DrawCommandMeta(DrawCommandId.Tilemap, RenderTargetId.Scene, 0);
        submitter.SubmitDraw(cmd, in meta);
    }
}