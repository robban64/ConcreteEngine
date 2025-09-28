using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Resources;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public readonly struct DrawCommand(MeshId meshId, MaterialId materialId, int drawCount = 0)
{
    public readonly MeshId MeshId = meshId;
    public readonly MaterialId MaterialId = materialId;
    public readonly int DrawCount = drawCount;
}

public readonly struct DrawTransformPayload(in Matrix4x4 transform)
{
    public readonly Matrix4x4 Transform = transform;
}

