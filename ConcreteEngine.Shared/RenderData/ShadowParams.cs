namespace ConcreteEngine.Shared.RenderData;

public readonly struct ShadowParams(
    int shadowMapSize,
    float distance,
    float zPad,
    float constBias,
    float slopeBias,
    float strength,
    float pcfRadius
)
{
    public readonly int ShadowMapSize  = shadowMapSize;
    public readonly float Distance  = distance;
    public readonly float ZPad  = zPad;
    public readonly float ConstBias  = constBias;
    public readonly float SlopeBias  = slopeBias;
    public readonly float Strength  = strength;
    public readonly float PcfRadius  = pcfRadius;

}