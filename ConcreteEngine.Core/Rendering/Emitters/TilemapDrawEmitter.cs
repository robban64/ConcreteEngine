#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Game.Terrain;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Graphics.Definitions;

#endregion

namespace ConcreteEngine.Core.Rendering.Emitters;

public sealed class TilemapDrawEmitter : DrawCommandEmitter<TilemapStruct>
{
    private List<DrawCommandLight> _lights =
    [
        new(new(500, 200), new(1.0f, 0.75f, 0.45f), 80.5f, 1.5f),
        new(new(200, 500), new(0.7f, 0.8f, 1.0f), 80f, 1.8f),
    ];

    private float tick = 0.01f;

    public TilemapDrawEmitter()
    {
        var r = Random.Shared;
        for (int i = 0; i < 12; i++)
        {
            var l = new DrawCommandLight(new(r.Next(0, 800), r.Next(0, 500)), new(1.0f, 0.75f, 0.45f), r.Next(40, 100),
                r.Next(1, 2));
            _lights.Add(l);
        }
    }


    protected override void EmitBatch(ReadOnlySpan<TilemapStruct> entities, in DrawEmitterContext ctx,
        DrawCommandSubmitter submitter, int order)
    {
        var transform = Transform2D.CreateTransformMatrix(Vector2.Zero, new Vector2(1, 1), 0);

        var tilemapBatcher = ctx.TilemapBatch;
        var result = tilemapBatcher.BuildBatch();
        var cmd = new DrawCommandMesh(
            meshId: result.GroundLayer.MeshId,
            drawCount: result.GroundLayer.DrawCount,
            materialId: MaterialId.Of(2),
            transform: in transform
        );

        var meta = new DrawCommandMeta(DrawCommandId.Tilemap, DrawCommandTag.SpriteRenderer, RenderTargetId.Scene,  0);
        submitter.SubmitDraw(in cmd, in meta);


        var lightSpan = CollectionsMarshal.AsSpan(_lights);
        for (int i = 0; i < lightSpan.Length; i++)
        {
            ref var light = ref lightSpan[i];
            var m = new DrawCommandMeta(DrawCommandId.Effect, DrawCommandTag.LightRenderer, RenderTargetId.SceneLight,  0);

            var td = tick > 0 ? 1 : -1;
            light.Position.Y += 2f * td;

            submitter.SubmitDraw(in light, in m);
        }

        tick += 0.1f;

        if (tick > 10)
            tick = -10;
    }
}