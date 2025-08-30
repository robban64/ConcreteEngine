using System.Numerics;
using ConcreteEngine.Core.Utils;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Scene.Entities;

public class SpriteClusterEntity : IGameEntity
{
    public GameEntityId Id { get; init; }
    public GameEntityInstanceId InstanceId { get; init; }
    public GameEntityStatus Status { get; set; }

    public SpriteClusterEntityUnit[] Entities { get; set; } = null!;
    public List<(int, int)> Batches { get; } = [];

    public ReadOnlySpan<SpriteClusterEntityUnit> GetBatch(int batch)
    {
        (int start, int size) = Batches[batch];
        var entities = Entities.AsSpan(start, size);
        return entities;
    }
}

public struct SpriteClusterEntityUnit
{
    public Vector2 Position = Vector2.Zero;
    public Vector2 PreviousPosition = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    public Vector2D<int> AtlasLocation = Vector2D<int>.Zero;
    public Vector2 Direction = Vector2.Zero;
    public UvRect Uv = default;

    public SpriteClusterEntityUnit()
    {
    }
}