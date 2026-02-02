namespace ConcreteEngine.Core.Engine.Assets;

public readonly struct MeshInfo(int vertexCount, int trisCount, byte meshIndex, byte materialIndex, byte numBones)
{
    public readonly int VertexCount = vertexCount;
    public readonly int TrisCount = trisCount;
    public readonly byte MeshIndex = meshIndex;
    public readonly byte MaterialIndex = materialIndex;
    public readonly byte NumBones = numBones;
}