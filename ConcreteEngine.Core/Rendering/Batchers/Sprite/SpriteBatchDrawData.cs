using System.Numerics;
using ConcreteEngine.Core.Utils;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Batchers;

public readonly record struct SpriteBatchDrawItem(
    Vector2 Position,
    Vector2 Scale,
    UvRect Uv);
    
public readonly struct SpriteBatchBuildResult(MeshId meshId, uint drawCount)
{
    public readonly MeshId MeshId = meshId;
    public readonly uint DrawCount = drawCount;
}
