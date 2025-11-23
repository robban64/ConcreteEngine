#region

using System.Numerics;
using System.Runtime.InteropServices;
using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Graphics.Primitives;

public interface IStd140Uniform;

[StructLayout(LayoutKind.Sequential)]
public readonly struct IVec4Std140(int x, int y = 0, int z = 0, int w = 0)
{
    public readonly int X = x, Y = y, Z = z, W = w;
}