using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct SkinningData
{
    public Int4 BoneIndices;
    public Vector4 BoneWeights;

    public static SkinningData Identity => new() { BoneIndices = new Int4(-1, -1, -1, -1), BoneWeights = default };
}