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

public readonly struct DrawCommandSkybox(TextureId textureId, ShaderId shaderId, in Matrix4x4 transform)
    : IDrawCommand
{
    public readonly TextureId TextureId = textureId;
    public readonly ShaderId ShaderId = shaderId;
    public readonly Matrix4x4 Transform = transform;
    public uint DrawCount { get; } = 36;
}


