using System.Numerics;
using ConcreteEngine.Core.Specs.Utils;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Worlds.Mesh.Sprite;

public readonly record struct SpriteBatchDrawItem(
    Vector2 Position,
    Vector2 Scale,
    UvRect Uv);

public readonly struct SpriteBatchBuildResult(MeshId meshId, uint drawCount)
{
    public readonly MeshId MeshId = meshId;
    public readonly uint DrawCount = drawCount;
}