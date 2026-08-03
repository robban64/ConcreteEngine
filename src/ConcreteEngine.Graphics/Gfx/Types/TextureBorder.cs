using System.Runtime.InteropServices;

namespace ConcreteEngine.Graphics.Gfx;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TextureBorder(byte r, byte g, byte b, byte a, bool enabled)
{
    public readonly byte R = r;
    public readonly byte G = g;
    public readonly byte B = b;
    public readonly byte A = a;
    public readonly bool Enabled = enabled;

    public static TextureBorder Off => new(0, 0, 0, 0, false);
    public static TextureBorder On => new(1, 1, 1, 1, true);
}