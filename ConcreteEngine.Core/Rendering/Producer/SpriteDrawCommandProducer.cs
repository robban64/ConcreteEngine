#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Features;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Transforms;
using ConcreteEngine.Core.Utils;

#endregion

namespace ConcreteEngine.Core.Rendering;

public readonly record struct SpriteDrawBatch(MaterialId MaterialId, int Start, int End, byte Layer = 0);

public struct SpriteDrawEntity(): IComparable<SpriteDrawEntity>
{
    public int SpriteId;
    public Vector2 Position = Vector2.Zero;
    public Vector2 PreviousPosition = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public UvRect Uv = default;
    public MaterialId MaterialId = default;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(SpriteDrawEntity other) => other.MaterialId.Id.CompareTo(MaterialId.Id);
}

public sealed class SpriteDrawProducer : DrawCommandProducer<SpriteFeatureDrawData>
{
    private static readonly Matrix4x4 DefaultTransform =
        Transform.CreateTransform2D(Vector2.Zero, Vector2.One, 0);
    
    private readonly struct SpriteBatchCache(
        MaterialId materialId,
        int length,
        in DrawCommandMesh cmd,
        in DrawCommandMeta meta)
    {
        public readonly MaterialId MaterialId = materialId;
        public readonly int Length = length;
        public readonly DrawCommandMesh Cmd = cmd;
        public readonly DrawCommandMeta Meta = meta;
    }

    public byte Layer { get; set; } = 1;

    private readonly List<SpriteBatchCache> _spriteBatches = [];

    private float _alpha = 0;
    

    public override void OnInitialize( CommandProducerContext ctx)
    {
        ctx.SpriteBatch.CreateSpriteBatch(0,1024);

    }

    protected override void EmitCommands(SpriteFeatureDrawData data,  CommandProducerContext ctx,
        DrawCommandSubmitter submitter, int order)
    {
        if (data.Count == 0) return;

        _alpha = ctx.Alpha;

        var batches = data.Batches;
        
        var spriteBatch = ctx.SpriteBatch;
        var entities = data.Entities.AsSpan();
        
        var batchIdx = 0;
        foreach (var (materialId, batchEnd) in batches)
        {
            var start = batchIdx == 0 ? 0 : batches[batchIdx - 1].Item2 + 1;
            var length = batchEnd - start;
            ProcessBatch(entities, spriteBatch, batchIdx, materialId, start, length);
            batchIdx++;
        }

        foreach (var batch in _spriteBatches)
        {
            submitter.SubmitDraw(in batch.Cmd, in batch.Meta);
        }

        _spriteBatches.Clear();
    }

    private void ProcessBatch(Span<SpriteDrawEntity> entities, SpriteBatcher sp, int batchIdx, MaterialId materialId, int start,int length)
    {
        var span = entities.Slice(start, length);
        

        sp.BeginBatch(batchIdx);
        foreach (ref readonly var entity in span)
        {
            var pos = Vector2.Lerp(entity.PreviousPosition, entity.Position, _alpha);
            var item = new SpriteBatchDrawItem(pos, entity.Scale, entity.Uv);
            sp.SubmitSprite(item);
        }
            
        var result = sp.BuildBatch();
        
        var meta = new DrawCommandMeta(DrawCommandId.Sprite, DrawCommandTag.SpriteRenderer,
            RenderTargetId.Scene, Layer);

        var cmd = new DrawCommandMesh(
            meshId: result.MeshId,
            materialId: materialId,
            drawCount: result.DrawCount,
            transform: in DefaultTransform
        );
        
        _spriteBatches.Add(new SpriteBatchCache(materialId, length, in cmd, in meta));

    }
}