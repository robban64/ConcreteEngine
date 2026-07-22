using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Editor;

namespace ConcreteEngine.Core.Engine.Graphics.Particles;

[StructLayout(LayoutKind.Sequential)]
public struct ParticleParams
{
    [InputColor] public ColorRgba StartColor;
    [InputColor] public ColorRgba EndColor;
    [InputNumber] public Vector2 SizeStartEnd;
    
    public static ParticleParams MakeDefault() =>
        new()
        {
            StartColor = new Color4(1.0f, 0.9f, 0.7f, 0.6f).ToRgba(),
            EndColor = new Color4(1.0f, 0.9f, 0.6f, 0.05f).ToRgba(),
            SizeStartEnd = new Vector2(0.12f, 0.22f),
        };
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out ColorRgba startColor, out ColorRgba endColor, out Vector2 sizeStartEnd)
    {
        startColor = StartColor;
        endColor = EndColor;
        sizeStartEnd = SizeStartEnd;
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct EmitterParams
{
    [InputNumber] public float Spread;
    [InputNumber] public Vector3 Direction;
    [InputNumber] public Vector2 SpeedMinMax;
    [InputNumber] public Vector2 LifeMinMax;

    public static EmitterParams MakeDefault() =>
        new()
        {
            Spread = 3.14f,
            LifeMinMax = new Vector2(6f, 10f),
            SpeedMinMax = new Vector2(0.02f, 0.05f)
        };
}