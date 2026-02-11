using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Lib;

namespace ConcreteEngine.Editor.Controller.Proxy;

public sealed class EditorLightProperties
{
    public readonly FloatDragField<Float3Value> Direction;
    public readonly ColorInputField Diffuse;
    public readonly FloatDragField<Float1Value> Intensity;
    public readonly FloatDragField<Float1Value> Specular;

    public readonly ColorInputField Ambient;
    public readonly ColorInputField AmbientGround;
    public readonly FloatDragField<Float1Value> Exposure;
}

public sealed class EditorFogProperties
{
    public readonly ColorInputField FogColor;
    public readonly FloatDragField<Float1Value> Density;
    
    public readonly FloatDragField<Float1Value> Base;
    public readonly FloatDragField<Float1Value> Falloff;
    public readonly FloatDragField<Float1Value> Influence;

    public readonly FloatDragField<Float1Value> Scattering;
    public readonly FloatDragField<Float1Value> MaxDistance;
    public readonly FloatDragField<Float1Value> Strength;
}

public sealed class EditorPostEffectProperties
{
    public readonly FloatSliderField<Float1Value> Exposure;
    public readonly FloatSliderField<Float1Value> Saturation;
    public readonly FloatSliderField<Float1Value> Contrast;
    public readonly FloatSliderField<Float1Value> Warmth;

    public readonly FloatSliderField<Float1Value> Tint;
    public readonly FloatSliderField<Float1Value> Strength;

    public readonly FloatSliderField<Float1Value> Intensity;
    public readonly FloatSliderField<Float1Value> Threshold;
    public readonly FloatSliderField<Float1Value> Radius;
    
    public readonly FloatSliderField<Float1Value> Vignette;
    public readonly FloatSliderField<Float1Value> Grain;
    public readonly FloatSliderField<Float1Value> xSharpen;
    public readonly FloatSliderField<Float1Value> Rolloff;
}

public sealed class EditorShadowProperties
{
    public readonly ComboField ShadowSize;

    public readonly FloatSliderField<Float1Value> Distance;
    public readonly FloatSliderField<Float1Value> ZPad;
    public readonly FloatSliderField<Float1Value> ConstBias;
    public readonly FloatSliderField<Float1Value> SlopeBias;

    public readonly FloatSliderField<Float1Value> Strength;
    public readonly FloatSliderField<Float1Value> PcfRadius;

}