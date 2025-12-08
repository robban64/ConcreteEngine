#region

using System.Numerics;
using ConcreteEngine.Engine.Utils;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Shared.Graphics;

#endregion

namespace ConcreteEngine.Engine.Worlds.MeshGeneration;

public readonly record struct SpriteBatchDrawItem(
    Vector2 Position,
    Vector2 Scale,
    UvRect Uv);

public readonly struct SpriteBatchBuildResult(MeshId meshId, uint drawCount)
{
    public readonly MeshId MeshId = meshId;
    public readonly uint DrawCount = drawCount;
}