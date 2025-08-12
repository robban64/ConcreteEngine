using System.Numerics;

namespace ConcreteEngine.Graphics.Data;

public readonly struct CreateMeshResult(ushort meshId, ushort vertexBufferId, ushort indexBufferId, uint drawCount)
{
    public readonly ushort MeshId = meshId;
    public readonly ushort VertexBufferId = vertexBufferId;
    public readonly ushort IndexBufferId = indexBufferId;
    public readonly uint DrawCount = drawCount;
}
