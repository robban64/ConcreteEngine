#region

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct SkinningData
{
    public Int4 BoneIndices;
    public Vector4 BoneWeights;

    public int GetVertexId(int idx)
    {
        if (idx < 0 || idx >= 4)
            throw new ArgumentOutOfRangeException(nameof(idx));

        ref var i0 = ref Unsafe.AsRef(ref BoneIndices.X);
        return Unsafe.Add(ref i0, idx);
    }

    public void Set(int idx, int vertexIndex, float boneWeight)
    {
        if (idx < 0 || idx >= 4)
            throw new ArgumentOutOfRangeException(nameof(idx));

        ref var i0 = ref Unsafe.AsRef(ref BoneIndices.X);
        Unsafe.Add(ref i0, idx) = vertexIndex;

        ref var w0 = ref Unsafe.AsRef(ref BoneWeights.X);
        Unsafe.Add(ref w0, idx) = boneWeight;
    }
}