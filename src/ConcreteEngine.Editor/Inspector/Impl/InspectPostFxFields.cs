using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.Lib.Field;
using static ConcreteEngine.Editor.EngineObjectStore;

namespace ConcreteEngine.Editor.Inspector.Impl;

internal sealed class InspectPostFxFields : InspectorFields<VisualManager>
{
    public readonly FloatCompositeField<Float4> GradeFields;
    public readonly FloatCompositeField<Float4> ImageFxFields;
    public readonly FloatCompositeField<Float3> BloomFields;
    public readonly FloatCompositeField<Float2> WbFields;

    public InspectPostFxFields() : base(segmentCount: 4)
    {
        GradeFields = Register(new FloatCompositeField<Float4>("Grade",
                static () =>
                {
                     var it =  Visuals.PostEffect.Grade;
                    return new Float4(it.Value.Exposure, it.Value.Saturation, it.Value.Contrast, it.Value.Warmth);
                },
                static value =>
                {
                    Visuals.PostEffect.Grade.Mutate = new PostGradeParams(value.X, value.Y, value.Z, value.W);
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithSlider("Exposure", 0.5f, 2f)
            .WithSlider("Saturation", 0f, 1.5f)
            .WithSlider("Contrast", 0f, 1.5f)
            .WithSlider("Warmth", 0f, 1f));

        ImageFxFields = Register(new FloatCompositeField<Float4>("Image Fx",
                static () =>
                {
                    var it =  Visuals.PostEffect.ImageFx;
                    return new Float4(it.Value.Vignette, it.Value.Grain, it.Value.Sharpen, it.Value.Rolloff);
                },
                static value =>
                {
                    Visuals.PostEffect.ImageFx.Mutate = new PostImageFxParams(value.X, value.Y, value.Z, value.W);
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithSlider("Vignette", 0f, 0.5f)
            .WithSlider("Grain", 0f, 0.5f)
            .WithSlider("Sharpen", 0f, 0.5f)
            .WithSlider("Rolloff", 0f, 0.5f));

        BloomFields = Register(new FloatCompositeField<Float3>("Bloom",
                static () =>
                {
                     var it =  Visuals.PostEffect.Bloom;
                    return new Float3(it.Value.Intensity, it.Value.Threshold, it.Value.Radius);
                },
                static value =>
                {
                    Visuals.PostEffect.Bloom.Mutate = new PostBloomParams(value.X, value.Y, value.Z);
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithSlider("Intensity", 0f, 2f)
            .WithSlider("Threshold", 0f, 2f)
            .WithSlider("Radius", 0f, 10f));

        WbFields = Register(new FloatCompositeField<Float2>("White Balance",
                static () =>
                {
                    var it = Visuals.PostEffect.WhiteBalance;
                    return new Float2(it.Value.Tint, it.Value.Strength);
                },
                static value =>
                {
                    Visuals.PostEffect.WhiteBalance.Mutate = new PostWhiteBalanceParams(value.X, value.Y);
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithSlider("Tint", 0f, 1f)
            .WithSlider("Strength", -1f, 1f));

        //
        CreateSegment("Grade", [GradeFields]);
        CreateSegment("White Balance", [WbFields]);
        CreateSegment("Bloom", [BloomFields]);
        CreateSegment("Image FX", [ImageFxFields]);
    }

    public override void Bind(VisualManager target) { }
}