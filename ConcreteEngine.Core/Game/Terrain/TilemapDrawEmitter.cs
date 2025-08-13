using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Tilemap;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Definitions;
using Silk.NET.Maths;

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
        var transform = Transform2D.CreateTransformMatrix(Vector2D<float>.Zero,new Vector2D<float>(1,1), 0);

        var tilemapBatcher = context.TilemapBatch;
        var result = tilemapBatcher.BuildBatch();
        var cmd = new TilemapDrawCommand(
            meshId: result.GroundLayer.MeshId,
            drawCount: result.GroundLayer.DrawCount,
            shaderId: Tilemap.TilemapShader.ResourceId,
            textureId: Tilemap.TilemapTexture.ResourceId,
            transform: in transform
        );
        submitter.SubmitDraw(cmd, new DrawCommandMeta(RenderTargetId.None, 0));

    }
}