using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Graphics.Primitives;

[StructLayout(LayoutKind.Sequential)]
public struct SkinningData
{
    public byte I0, I1, I2, I3;
    // normalized weights [0, 255] -> [0, 1] in shader
    public byte W0, W1, W2, W3;
}