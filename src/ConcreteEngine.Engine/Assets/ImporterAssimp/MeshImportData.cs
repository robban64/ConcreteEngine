using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Assets.ImporterAssimp;

internal struct MeshImportData
{
    public NativeView<Vector3> Positions;
    public NativeView<VertexShading> Vertices;
    public NativeView<byte> Indices;
    public NativeView<SkinningData> Skinning;
}
