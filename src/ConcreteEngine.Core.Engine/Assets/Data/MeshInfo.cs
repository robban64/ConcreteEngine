namespace ConcreteEngine.Core.Engine.Assets;

public readonly struct MeshInfo(int vertexCount, int trisCount, byte meshIndex, byte materialIndex, ushort numBones)
{
    public readonly int VertexCount = vertexCount;
    public readonly int TrisCount = trisCount;
    public readonly ushort NumBones = numBones;
    public readonly byte MeshIndex = meshIndex;
    public readonly byte MaterialIndex = materialIndex;
}