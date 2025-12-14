using System.Runtime.InteropServices;

namespace ConcreteEngine.Shared.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct ShadowParams(
    int shadowMapSize,
    float distance,
    float zPad,
    float constBias,
    float slopeBias,
    float strength,
    float pcfRadius)
{
    public int ShadowMapSize = shadowMapSize;
    public float Distance = distance;
    public float ZPad = zPad;
    public float ConstBias = constBias;
    public float SlopeBias = slopeBias;
    public float Strength = strength;
    public float PcfRadius = pcfRadius;
}