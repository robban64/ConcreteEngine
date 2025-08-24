#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Definitions;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering;


public enum DrawCommandId : byte
{
    Invalid,
    Tilemap,
    Sprite,
    Effect
}

public enum DrawCommandTag : byte
{
    Invalid,
    SpriteRenderer,
    LightRenderer
}


public interface IDrawCommand
{
    uint DrawCount { get; }
}

public readonly struct CommandContainer<T>(in T cmd, in DrawCommandMeta meta, int submitIdx)
    : IComparable<CommandContainer<T>> where T : struct, IDrawCommand
{
    public readonly T Cmd = cmd;
    public readonly DrawCommandMeta Meta = meta;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(CommandContainer<T> other) => other.Meta.CompareTo(Meta);
}

public readonly struct DrawCommandMesh(
    MeshId meshId,
    MaterialId materialId,
    uint drawCount,
    in Matrix4x4 transform) : IDrawCommand
{
    public readonly MeshId MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly Matrix4x4 Transform = transform;
    public uint DrawCount { get; } = drawCount;
}

public struct DrawCommandLight(Vector2 position, Vector3 color, float radius, float intensity) : IDrawCommand
{
    public Vector2 Position = position;
    public Vector3 Color = color;
    public float Radius = radius;
    public float Intensity = intensity;
    public uint DrawCount => 4;
}

public readonly struct DrawCommandMeta
{
    public readonly DrawCommandId Id;
    public readonly DrawCommandTag Tag;
    public readonly RenderTargetId Target;
    public readonly byte Layer;
    private readonly ushort _sortKey;

    public DrawCommandMeta(DrawCommandId id,DrawCommandTag tag, RenderTargetId target, byte layer)
    {
        Id = id;
        Tag = tag;
        Target = target;
        Layer = layer;
        _sortKey = (ushort)(((byte)target << 8) | layer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(DrawCommandMeta other) => other._sortKey.CompareTo(_sortKey);
}