using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Engine.Assets.Loader.Data;


public sealed class ModelImportData(int meshCount)
{
    public int TotalVertexCount;
    public int TotalFaceCount;

    public BoundingBox ModelBounds;
    public readonly MeshEntry[] Meshes = new MeshEntry[meshCount];
    public readonly Matrix4x4[] WorldTransforms = new Matrix4x4[meshCount];
}
