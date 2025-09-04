using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public interface IDrawCommand
{
    uint DrawCount { get; }
}

public readonly struct DrawCommandMesh(MeshId meshId, MaterialId materialId, uint drawCount, in Matrix4x4 transform)
    : IDrawCommand
{
    public readonly MeshId MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly Matrix4x4 Transform = transform;
    public uint DrawCount { get; } = drawCount;
}


public readonly struct DrawCommandTerrain(MeshId meshId, MaterialId materialId, uint drawCount, in Matrix4x4 transform)
    : IDrawCommand
{
    public readonly MeshId MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly Matrix4x4 Transform = transform;
    public uint DrawCount { get; } = drawCount;
}

public readonly struct DrawCommandSprite(MeshId meshId, MaterialId materialId, uint drawCount, in Matrix4x4 transform)
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