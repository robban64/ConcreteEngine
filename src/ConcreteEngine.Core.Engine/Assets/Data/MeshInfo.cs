using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Assets.Data;

[Inspectable]
public readonly struct ModelInfo(int vertexCount, int faceCount, ushort boneCount, byte meshCount, byte materialCount, byte textureCount, bool isAnimated)
{
    public readonly int VertexCount = vertexCount;
    public readonly int FaceCount = faceCount;
    public readonly ushort BoneCount = boneCount;
    public readonly byte MeshCount = meshCount;
    public readonly byte MaterialCount = materialCount;
    public readonly byte TextureCount = textureCount;
    public readonly bool IsAnimated = isAnimated;
}

[Inspectable]
public readonly struct MeshInfo(int vertexCount, int trisCount, byte meshIndex, byte materialIndex, ushort boneCount)
{
    public readonly int VertexCount = vertexCount;
    public readonly int TrisCount = trisCount;
    public readonly ushort BoneCount = boneCount;
    public readonly byte MeshIndex = meshIndex;
    public readonly byte MaterialIndex = materialIndex;
}