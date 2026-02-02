using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Engine.Assets.Loader.ImporterAssimp;

//
public sealed class ModelData(int meshCount)
{
    public int TotalVertexCount;
    public int TotalFaceCount;

    public BoundingBox ModelBounds;
    public readonly MeshEntry[] Meshes = new MeshEntry[meshCount];
    public readonly Matrix4x4[] WorldTransforms = new Matrix4x4[meshCount];

    //public readonly Matrix4x4[] LocalTransforms = new Matrix4x4[meshCount];
}

