#region

using System.Numerics;
using ConcreteEngine.Core.Utils;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Features.Sprite;

public sealed class SpriteFeatureDrawData
{
    public SpriteDrawEntity[] Entities { get; set; } = null!;
    public List<(int, int)> Batches { get; set; } = null!;

    public ReadOnlySpan<SpriteDrawEntity> GetBatch(int batch)
    {
        (int start, int size) = Batches[batch];
        var entities = Entities.AsSpan(start, size);
        return entities;
    }
}

public struct SpriteDrawEntity
{
    public Vector2 Position = Vector2.Zero;
    public Vector2 PreviousPosition = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public Vector2D<int> AtlasLocation = Vector2D<int>.Zero;
    public Vector2 Direction = Vector2.Zero;
    public UvRect Uv = default;

    public SpriteDrawEntity()
    {
    }
}


