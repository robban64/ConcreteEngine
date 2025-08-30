#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Core.Utils;

#endregion

namespace ConcreteEngine.Core.Features.Sprite;

public sealed class SpriteFeatureDrawData
{
    public List<SpriteDrawEntity> Entities { get; set; } = null!;
    public List<SpriteDrawBatchData> Batches { get; set; } = null!;

    public ReadOnlySpan<SpriteDrawEntity> GetBatch(int batch)
    {
        var batchData  = Batches[batch];
        var entities = CollectionsMarshal.AsSpan(Entities).Slice(batchData.Start, batchData.End);
        return entities;
    }
}

public readonly record struct SpriteDrawBatchData(MaterialId MaterialId, int Start, int End, byte Layer = 0);

public struct SpriteDrawEntity()
{
    public Vector2 Position = Vector2.Zero;
    public Vector2 PreviousPosition = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public UvRect Uv = default;
    public MaterialId MaterialId = default;
}