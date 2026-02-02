namespace ConcreteEngine.Core.Engine.Assets;

public readonly struct ModelInfo
{
    public readonly int VertexCount;
    public readonly int FaceCount;
    public readonly ushort BoneCount;
    public readonly byte MeshCount;
    public readonly byte MaterialCount;
    public readonly byte TextureCount;
    public readonly bool IsAnimated;
}

public readonly struct MeshInfo(int vertexCount, int trisCount, byte meshIndex, byte materialIndex, ushort boneCount)
{
    public readonly int VertexCount = vertexCount;
    public readonly int TrisCount = trisCount;
    public readonly ushort BoneCount = boneCount;
    public readonly byte MeshIndex = meshIndex;
    public readonly byte MaterialIndex = materialIndex;
}