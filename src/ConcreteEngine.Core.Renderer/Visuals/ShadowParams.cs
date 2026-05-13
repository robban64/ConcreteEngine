using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Renderer.Visuals;

public struct ShadowProjectionParams(float distance, float zPad, float constBias, float slopeBias)
{
    public float Distance = distance;
    public float ZPad = zPad;
    public float ConstBias = constBias;
    public float SlopeBias = slopeBias;
}

public struct ShadowVisualParams(float strength, float pcfRadius)
{
    public float Strength = strength;
    public float PcfRadius = pcfRadius;
}