#region

using System.Numerics;
using ConcreteEngine.Core.Assets.Resources;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Commands;

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

public readonly struct DrawCommandMeta(
    DrawCommandId id,
    DrawCommandQueue queue,
    PassMask passMask = PassMask.Default,
    ushort depthKey = 0)
{
    public readonly PassMask PassMask = passMask;
    public readonly ushort DepthKey = depthKey;
    public readonly DrawCommandId Id = id;
    public readonly DrawCommandQueue Queue = queue;
}