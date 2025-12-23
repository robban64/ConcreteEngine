using System.Numerics;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Shared.Graphics;

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