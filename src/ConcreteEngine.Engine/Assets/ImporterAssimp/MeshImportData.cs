using System.Numerics;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Graphics.Primitives;

namespace ConcreteEngine.Engine.Assets.ImporterAssimp;

internal sealed class MeshImportData
{
    public NativeView<Vector3> Positions;
    public NativeView<VertexShading> Vertices;
    public NativeView<byte> Indices;
    public NativeView<SkinningData> Skinning;

    public void Clear()
    {
        Positions = default;
        Vertices = default;
        Indices = default;
        Skinning = default;
    }
}