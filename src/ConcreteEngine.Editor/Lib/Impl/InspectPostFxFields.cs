using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Core.Renderer.Visuals;
using ConcreteEngine.Editor.Lib.Field;
using static ConcreteEngine.Editor.EngineObjectStore;

namespace ConcreteEngine.Editor.Lib.Impl;

internal sealed class InspectPostFxFields : InspectorFields<VisualEnvironment>
{
    public readonly FloatCompositeField<Float4Value> GradeFields;
    public readonly FloatCompositeField<Float4Value> ImageFxFields;
    public readonly FloatCompositeField<Float3Value> BloomFields;
    public readonly FloatCompositeField<Float2Value> WbFields;

    public InspectPostFxFields() : base(segmentCount: 4)
    {
        GradeFields = Register(new FloatCompositeField<Float4Value>("Grade",
                static () =>
                {
                    ref readonly var it = ref Visuals.GetPostEffect().Grade;
                    return new Float4Value(it.Exposure, it.Saturation, it.Contrast, it.Warmth);
                },
                static value =>
                {
                    Visuals.Mutate().PostEffect.Grade = new PostGradeParams(value.X, value.Y, value.Z, value.W);
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithSlider("Exposure", 0.5f, 2f)
            .WithSlider("Saturation", 0f, 1.5f)
            .WithSlider("Contrast", 0f, 1.5f)
            .WithSlider("Warmth", 0f, 1f));

        ImageFxFields = Register(new FloatCompositeField<Float4Value>("Image Fx",
                static () =>
                {
                    ref readonly var it = ref Visuals.GetPostEffect().ImageFx;
                    return new Float4Value(it.Vignette, it.Grain, it.Sharpen, it.Rolloff);
                },
                static value =>
                {
                    Visuals.Mutate().PostEffect.ImageFx = new PostImageFxParams(value.X, value.Y, value.Z, value.W);
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithSlider("Vignette", 0f, 0.5f)
            .WithSlider("Grain", 0f, 0.5f)
            .WithSlider("Sharpen", 0f, 0.5f)
            .WithSlider("Rolloff", 0f, 0.5f));

        BloomFields = Register(new FloatCompositeField<Float3Value>("Bloom",
                static () =>
                {
                    ref readonly var it = ref Visuals.GetPostEffect().Bloom;
                    return new Float3Value(it.Intensity, it.Threshold, it.Radius);
                },
                static value =>
                {
                    Visuals.Mutate().PostEffect.Bloom = new PostBloomParams(value.X, value.Y, value.Z);
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithSlider("Intensity", 0f, 2f)
            .WithSlider("Threshold", 0f, 2f)
            .WithSlider("Radius", 0f, 10f));

        WbFields = Register(new FloatCompositeField<Float2Value>("White Balance",
                static () =>
                {
                    var it = Visuals.GetPostEffect().WhiteBalance;
                    return new Float2Value(it.Tint, it.Strength);
                },
                static value =>
                {
                    Visuals.Mutate().PostEffect.WhiteBalance = new PostWhiteBalanceParams(value.X, value.Y);
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

    public override void Bind(VisualEnvironment target) { }
}