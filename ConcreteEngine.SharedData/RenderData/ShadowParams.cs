namespace ConcreteEngine.Shared.RenderData;

public readonly record struct ShadowParams(
    int ShadowMapSize,
    float Distance,
    float ZPad,
    float ConstBias,
    float SlopeBias,
    float Strength,
    float PcfRadius
);