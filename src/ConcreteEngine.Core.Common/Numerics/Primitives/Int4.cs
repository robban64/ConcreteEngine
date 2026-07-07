using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Int4(int x, int y, int z, int w)
{
    public int X = x;
    public int Y = y;
    public int Z = z;
    public int W = w;

    public static Int4 One => new(1, 1, 1, 1);
    public static Int4 NegativeOne => new(-1, -1, -1, -1);
}