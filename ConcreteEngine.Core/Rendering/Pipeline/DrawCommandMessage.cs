#region

using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Pipeline;

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

public readonly struct DrawCommandMeta(DrawCommandId id, DrawCommandTag tag, RenderTargetId target, byte layer)
{
    public readonly DrawCommandId Id = id;
    public readonly DrawCommandTag Tag = tag;
    public readonly RenderTargetId Target = target;
    public readonly byte Layer = layer;
}

public readonly struct DrawCommandMesh(MeshId meshId, MaterialId materialId, uint drawCount, in Matrix4x4 transform)
    : IDrawCommand
{
    public readonly MeshId MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly Matrix4x4 Transform = transform;
    public uint DrawCount { get; } = drawCount;
}

//TODO Make read only
public struct DrawCommandLight(Vector2 position, Vector3 color, float radius, float intensity) : IDrawCommand
{
    public Vector2 Position = position;
    public Vector3 Color = color;
    public float Radius = radius;
    public float Intensity = intensity;
    public uint DrawCount => 4;
}