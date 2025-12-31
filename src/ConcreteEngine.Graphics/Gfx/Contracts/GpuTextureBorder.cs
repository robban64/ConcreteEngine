using System.Runtime.InteropServices;

namespace ConcreteEngine.Graphics.Gfx.Contracts;

[StructLayout(LayoutKind.Sequential)]
public readonly struct GpuTextureBorder(byte r, byte g, byte b, byte a, bool enabled)
{
    public readonly byte R = r;
    public readonly byte G = g;
    public readonly byte B = b;
    public readonly byte A = a;
    public readonly bool Enabled = enabled;

    public static GpuTextureBorder Off => new(0, 0, 0, 0, false);
    public static GpuTextureBorder On => new(1, 1, 1, 1, true);
}