#region

using System.Runtime.InteropServices;

#endregion

namespace ConcreteEngine.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Int4(int x, int y, int z, int w)
{
    public int X = x;
    public int Y = y;
    public int Z = z;
    public int W = w;
}