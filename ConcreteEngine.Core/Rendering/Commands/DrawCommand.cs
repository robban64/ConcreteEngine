using System.Numerics;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public readonly struct DrawCommand(MeshId meshId, MaterialId materialId, in Matrix4x4 transform, uint drawCount = 0)
{
    public readonly MeshId MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly Matrix4x4 Transform = transform;
    public readonly uint DrawCount = drawCount;
}
