using System.Numerics;

namespace ConcreteEngine.Graphics.Data;

public readonly struct CreateMeshResult(int meshId, int vertexBufferId, int indexBufferId, uint drawCount)
{
    public readonly int MeshId = meshId;
    public readonly int VertexBufferId = vertexBufferId;
    public readonly int IndexBufferId = indexBufferId;
    public readonly uint DrawCount = drawCount;
}
