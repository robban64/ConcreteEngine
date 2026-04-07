using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Assets.Loader.Data;

public sealed class ModelImportData(int meshCount)
{
    public readonly MeshEntry[] Meshes = new MeshEntry[meshCount];
    public readonly ArenaBlockPtr[] Blocks = new ArenaBlockPtr[meshCount];

    public BoundingBox ModelBounds;
    public int TotalVertexCount;
    public int TotalFaceCount;

    public void GetMeshData(
        int meshIndex,
        out NativeView<Vertex3D> vertices,
        out NativeView<SkinningData> skinned,
        out NativeView<uint> indices)
    {
        var mesh = Meshes[meshIndex];
        var block = Blocks[meshIndex];

        vertices = block.DataPtr.Reinterpret<Vertex3D>();
        block = block.Next;

        if (block.IsNull) throw new InvalidOperationException("Index block is null");

        indices = block.DataPtr.Reinterpret<uint>();
        block = block.Next;

        if (mesh.Info.BoneCount > 0)
        {
            if (block.IsNull) throw new InvalidOperationException("Skinned block is null");
            skinned = block.DataPtr.Reinterpret<SkinningData>();
        }
        else
        {
            skinned = default;
        }
    }
}