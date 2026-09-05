using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

[StructLayout(LayoutKind.Sequential)]
public readonly struct ParticleVisualState(float size, ColorRgba color)
{
    public readonly float Size = size;
    public readonly ColorRgba Color = color;
}