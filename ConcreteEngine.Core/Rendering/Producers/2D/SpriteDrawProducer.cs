#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Assets.Materials;
using ConcreteEngine.Core.Utils;

#endregion

namespace ConcreteEngine.Core.Rendering.Producers;

public readonly record struct SpriteDrawBatch(MaterialId MaterialId, int Start, int End, byte Layer = 0);

public struct SpriteDrawEntity() : IComparable<SpriteDrawEntity>
{
    public Vector2 Position = Vector2.Zero;
    public Vector2 PreviousPosition = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public UvRect Uv = default;
    public MaterialId MaterialId = default;
    public int SpriteId;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(SpriteDrawEntity other) => other.MaterialId.Id.CompareTo(MaterialId.Id);
}
/*
public interface ISpriteDrawSink : IDrawSink
{
    void Send(ReadOnlySpan<SpriteDrawEntity> payload);
    void BuildBatches(List<(MaterialId, int)> batches);
}

public sealed class SpriteDrawProducer : IDrawCommandProducer, ISpriteDrawSink
{
    private static readonly Matrix4x4 DefaultTransform =
        TransformHelper.CreateTransform2D(Vector2.Zero, Vector2.One, 0);


    private readonly List<SpriteBatchCache> _spriteBatches = [];
    private List<(MaterialId, int)> _batches = [];

    private SpriteBatcher _spriteBatch = null!;

    private CommandProducerContext _context = null!;

    private int _idx = 0;
    private SpriteDrawEntity[] _entities = new SpriteDrawEntity[32];


    private UpdateMetaInfo _updateMeta;

    public void AttachContext(CommandProducerContext ctx)
    {
        _context = ctx;
    }

    public void Initialize( )
    {
        _spriteBatch = _context.DrawBatchers.Get<SpriteBatcher>();
        _spriteBatch.CreateSpriteBatch(0,1024);

    }

    public void Send(ReadOnlySpan<SpriteDrawEntity> payload)
    {
        EnsureCapacity(payload.Length);
        var entities = _entities.AsSpan();
        foreach (ref readonly var entity in payload)
        {
            entities[_idx++] = entity;
        }
    }

    public void BuildBatches(List<(MaterialId, int)> batches)
    {
        _batches = batches;
    }

    public void BeginTick(in UpdateMetaInfo updateMeta)
    {
        _idx = 0;
        _updateMeta = updateMeta;
    }

    public void EndTick()
    {
    }

    public void EmitFrame(float alpha, IRenderPipeline submitter)
    {
        if (_idx == 0) return;

        var entities = _entities.AsSpan();

        var batchIdx = 0;
        foreach (var (materialId, batchEnd) in _batches)
        {
            var start = batchIdx == 0 ? 0 : _batches[batchIdx - 1].Item2 + 1;
            var length = batchEnd - start;
            ProcessBatch(entities, alpha, batchIdx, materialId, start, length);
            batchIdx++;
        }

        foreach (var batch in _spriteBatches)
        {
            submitter.SubmitDraw(in batch.Cmd, in batch.Meta);
        }

        _spriteBatches.Clear();
    }

    private void ProcessBatch(Span<SpriteDrawEntity> entities, float alpha, int batchIdx, MaterialId materialId, int start,int length)
    {
        var span = entities.Slice(start, length);

        _spriteBatch.BeginBatch(batchIdx);
        foreach (ref readonly var entity in span)
        {
            var pos = Vector2.Lerp(entity.PreviousPosition, entity.Position, alpha);
            var item = new SpriteBatchDrawItem(pos, entity.Scale, entity.Uv);
            _spriteBatch.SubmitSprite(item);
        }

        var result = _spriteBatch.BuildBatch();

        var meta = DrawCommandMeta.Make2D(DrawCommandId.Sprite,  RenderTargetId.Scene, 1);

        var cmd = new DrawCommandSprite(
            meshId: result.MeshId,
            materialId: materialId,
            drawCount: result.DrawCount,
            transform: in DefaultTransform
        );

        _spriteBatches.Add(new SpriteBatchCache(materialId, length, in cmd, in meta));

    }

    private void EnsureCapacity(int size)
    {
        if (_entities.Length < size + 1)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(size, 50_000);
            var newSize = int.Max(_entities.Length * 2, size);
            Array.Resize(ref _entities, newSize);
        }
    }

    private readonly struct SpriteBatchCache(
        MaterialId materialId,
        int length,
        in DrawCommandSprite cmd,
        in DrawCommandMeta meta)
    {
        public readonly MaterialId MaterialId = materialId;
        public readonly int Length = length;
        public readonly DrawCommandSprite Cmd = cmd;
        public readonly DrawCommandMeta Meta = meta;
    }


}*/