#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Utils;

#endregion

namespace ConcreteEngine.Core.Features.Sprite;

public sealed class SpriteFeatureDrawData
{
    public List<SpriteDrawEntity> Entities { get; set; } = null!;
    public List<(int, int)> Batches { get; set; } = null!;

    public ReadOnlySpan<SpriteDrawEntity> GetBatch(int batch)
    {
        (int start, int size) = Batches[batch];
        var entities = CollectionsMarshal.AsSpan(Entities).Slice(start, size);
        return entities;
    }
}

public struct SpriteDrawEntity()
{
    public Vector2 Position = Vector2.Zero;
    public Vector2 PreviousPosition = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public UvRect Uv = default;
}