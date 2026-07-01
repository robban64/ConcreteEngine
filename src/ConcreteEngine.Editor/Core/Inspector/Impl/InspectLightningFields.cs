using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Editor.Lib.Field;

namespace ConcreteEngine.Editor.Core.Inspector.Impl;

internal sealed class InspectLightningFields : InspectorFields<VisualManager>
{
    private static VisualManager Visuals => VisualManager.Instance;

    public readonly FloatField<Float3> Direction;
    public readonly ColorField Diffuse;
    public readonly FloatField<Float1> Intensity;
    public readonly FloatField<Float1> Specular;

    //
    public readonly ColorField Ambient;
    public readonly ColorField AmbientGround;
    public readonly FloatField<Float1> Exposure;

    //
    public readonly ComboField ShadowSizeCombo;
    public readonly FloatCompositeField<Float4> ShadowProjectionFields;
    public readonly FloatCompositeField<Float2> ShadowVisualFields;

    //
    public readonly ColorField FogColorField;
    public readonly FloatCompositeField<Float4> FogHeightFields;
    public readonly FloatCompositeField<Float3> FogOpticsFields;

    public InspectLightningFields() : base(segmentCount: 6)
    {
        Direction = Register(new FloatField<Float3>("Direction", FieldWidgetKind.Drag,
            static () => Visuals.Illumination.DirectionalLight.Value.Direction,
            static value => Visuals.Illumination.DirectionalLight.Mutate.Direction = (Vector3)value)
        {
            Format = "%.3f", Speed = 0.01f, Min = -1f, Max = 1f
        });

        Diffuse = Register(new ColorField("Diffuse", false,
            static () => (Color4)Visuals.Illumination.DirectionalLight.Value.Diffuse,
            static value => Visuals.Illumination.DirectionalLight.Mutate.Diffuse = (Vector3)value
        ));

        Intensity = Register(new FloatField<Float1>("Intensity", FieldWidgetKind.Drag,
            static () => Visuals.Illumination.DirectionalLight.Value.Intensity,
            static value => Visuals.Illumination.DirectionalLight.Mutate.Intensity = (float)value
        )
        {
            Format = "%.3f",
            Speed = 0.01f,
            Min = 0f,
            Max = 10f,
            Layout = FieldLayout.Inline
        });
        Specular = Register(new FloatField<Float1>("Specular", FieldWidgetKind.Drag,
            static () => Visuals.Illumination.DirectionalLight.Value.Specular,
            static value => Visuals.Illumination.DirectionalLight.Mutate.Specular = (float)value)
        {
            Format = "%.3f",
            Delay = FieldGetDelay.High,
            Speed = 0.01f,
            Min = 0f,
            Max = 10f,
            Layout = FieldLayout.Inline
        });

        // Ambient
        Ambient = Register(new ColorField("Ambient", false,
            static () => Visuals.Illumination.Ambient.Value.Ambient,
            static value => Visuals.Illumination.Ambient.Mutate.Ambient = (Color4)value
        ));
        AmbientGround = Register(new ColorField("Ambient Ground", false,
            static () => Visuals.Illumination.Ambient.Value.AmbientGround,
            static value => Visuals.Illumination.Ambient.Mutate.AmbientGround = (Color4)value
        ));
        Exposure = Register(new FloatField<Float1>("Exposure", FieldWidgetKind.Drag,
            static () => Visuals.Illumination.Ambient.Value.Exposure,
            static value => Visuals.Illumination.Ambient.Mutate.Exposure = (float)value)
        {
            Format = "%.3f",
            Delay = FieldGetDelay.High,
            Speed = 0.01f,
            Min = 0f,
            Max = 2f,
            Layout = FieldLayout.Inline
        });

        // Shadow
        ShadowSizeCombo = Register(new ComboField("Shadow Size",
            [1024, 2048, 4096, 8192], ["1024px", "2048px", "4096px", "8192px"],
            static () => Visuals.Shadow.ShadowMapSize,
            static value => Visuals.Shadow.ShadowMapSize = (int)value
        ).WithProperties(FieldGetDelay.VeryHigh, FieldLayout.None).WithPlaceholder("No Shadow"));

        ShadowProjectionFields = Register(new FloatCompositeField<Float4>(
                "Shadow Projection",
                static () =>
                {
                    var it = Visuals.Shadow.Projection;
                    return new Float4(it.Value.Distance, it.Value.ZPad, it.Value.ConstBias, it.Value.SlopeBias);
                },
                static value =>
                {
                    ref var it = ref Visuals.Shadow.Projection.Mutate;
                    it.Distance = value.X;
                    it.ZPad = value.Y;
                    it.ConstBias = value.Z;
                    it.SlopeBias = value.W;
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithSlider("Distance", 10f, 500f)
            .WithSlider("Z-Padding", 0f, 100f)
            .WithDrag("Const Bias", 0.001f, 0.0001f, 0.01f, "%.4f")
            .WithDrag("Slope Bias", 0.001f, 0.001f, 0.01f, "%.4f"));

        ShadowVisualFields = Register(new FloatCompositeField<Float2>(
                "Shadow Visual",
                static () =>
                {
                    var it = Visuals.Shadow.Visuals;
                    return new Float2(it.Value.Strength, it.Value.PcfRadius);
                },
                static value =>
                {
                    ref var it = ref Visuals.Shadow.Visuals.Mutate;
                    it.Strength = value.X;
                    it.PcfRadius = value.Y;
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithSlider("Strength", 0f, 1f).WithSlider("PcfRadius", 0.5f, 4f));


        // Fog
        FogColorField = Register(new ColorField("FogColor", false,
                static () => Visuals.Environment.FogOptics.Value.Color,
                static value => Visuals.Environment.FogOptics.Mutate.Color = (Color4)value)
            .WithProperties(FieldGetDelay.VeryHigh));

        FogHeightFields = Register(new FloatCompositeField<Float4>(
                "Fog Height",
                static () =>
                {
                    ref readonly var f = ref Visuals.Environment.FogHeight.Value;
                    var heightWeight = Visuals.Environment.FogOptics.Value.HeightWeight;
                    return new Float4(f.Density, f.BaseHeight, f.HeightFalloff, heightWeight);
                },
                static value =>
                {
                    ref var o = ref Visuals.Environment.FogOptics.Mutate;
                    ref var h = ref Visuals.Environment.FogHeight.Mutate;

                    h.Density = value.X;
                    h.BaseHeight = value.Y;
                    h.HeightFalloff = value.Z;
                    o.HeightWeight = value.W;
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithSlider("Density", 100, 1500, "%.5f").WithSlider("BaseHeight", -1000f, 1000f, "%.3f")
            .WithSlider("Falloff", 0.001f, 10000.0f, "%.3f").WithDrag("Influence", 0.001f, 0f, 1f, "%.3f"));

        FogOpticsFields = Register(new FloatCompositeField<Float3>(
                "Fog Optics",
                static () =>
                {
                    ref readonly var o = ref Visuals.Environment.FogOptics.Value;
                    ref readonly var h = ref Visuals.Environment.FogHeight.Value;

                    return new Float3(o.Scattering, h.Strength, h.MaxDistance);
                },
                static value =>
                {
                    ref var o = ref Visuals.Environment.FogOptics.Mutate;
                    ref var h = ref Visuals.Environment.FogHeight.Mutate;

                    o.Scattering = value.X;
                    h.Strength = value.Y;
                    h.MaxDistance = value.Z;
                })
            .WithProperties(FieldGetDelay.VeryHigh)
            .WithDrag("Scattering", 0.001f, 0f, 1f, "%.5f").WithDrag("Strength", 0.001f, 0f, 1f, "%.3f")
            .WithDrag("Distance", 1, 1f, 10000f, "%.0f"));

        CreateSegment("Directional Light", [Direction, Diffuse, Intensity, Specular]);
        CreateSegment("Ambient Light", [Ambient, AmbientGround, Exposure]);

        CreateSegment("Shadow Map Size", [ShadowSizeCombo]);
        CreateSegment("Shadow Projection", [ShadowProjectionFields]);
        CreateSegment("Shadow Visuals", [ShadowVisualFields]);

        CreateSegment("Fog Effect", [FogColorField, FogHeightFields, FogOpticsFields]);
    }

    public override void Bind(VisualManager target) { }
}