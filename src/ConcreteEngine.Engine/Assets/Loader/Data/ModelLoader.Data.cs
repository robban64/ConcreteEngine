using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;

namespace ConcreteEngine.Engine.Assets.Loader.Data;

public sealed class ModelImportData(int meshCount)
{
    public int TotalVertexCount;
    public int TotalFaceCount;
    public Matrix4x4 InverseRoot;
    public BoundingBox ModelBounds;
    public readonly MeshEntry[] Meshes = new MeshEntry[meshCount];
}