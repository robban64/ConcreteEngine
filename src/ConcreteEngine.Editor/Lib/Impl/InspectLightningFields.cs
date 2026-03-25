using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Renderer;
using static ConcreteEngine.Editor.Bridge.EngineObjectStore;

namespace ConcreteEngine.Editor.Lib.Impl;


internal sealed class InspectLightningFields : InspectorFields<VisualEnvironment>
{
    public readonly FloatField<Float3Value> Direction;
    public readonly ColorField Diffuse;
    public readonly FloatField<Float1Value> Intensity;
    public readonly FloatField<Float1Value> Specular;

    //
    public readonly ColorField Ambient;
    public readonly ColorField AmbientGround;
    public readonly FloatField<Float1Value> Exposure;

    //
    public readonly ComboField ShadowSizeCombo;
    public readonly FloatGroupField<Float4Value> ShadowProjectionFields;
    public readonly FloatGroupField<Float2Value> ShadowVisualFields;

    //
    public readonly ColorField FogColorField;
    public readonly FloatGroupField<Float4Value> FogHeightFields;
    public readonly FloatGroupField<Float3Value> FogOpticsFields;
    
    public InspectLightningFields(): base(segmentCount: 6)
    {
        Direction = Register(new FloatField<Float3Value>("Direction", FieldWidgetKind.Drag,
            static () => Visuals.GetDirectionalLight().Direction,
            static value => Visuals.Mutate().SunLight.Direction = (Vector3)value)
        {
            Format = "%.3f",
            Speed = 0.01f,
            Min = -1f,
            Max = 1f
        });

        Diffuse = Register(new ColorField("Diffuse", false,
            static () => (Color4)Visuals.GetDirectionalLight().Diffuse,
            static value => Visuals.Mutate().SunLight.Diffuse = (Vector3)value
        ));

        Intensity = Register(new FloatField<Float1Value>("Intensity", FieldWidgetKind.Drag,
            static () => Visuals.GetDirectionalLight().Intensity,
            static value => Visuals.Mutate().SunLight.Intensity = (float)value
        )
        {
            Format = "%.3f",
            Speed = 0.01f,
            Min = 0f,
            Max = 10f,
            Layout = FieldLayout.Inline
        });
        Specular = Register(new FloatField<Float1Value>("Specular", FieldWidgetKind.Drag,
            static () => Visuals.GetDirectionalLight().Specular,
            static value => Visuals.Mutate().SunLight.Specular = (float)value)
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
            static () => (Color4)Visuals.GetAmbient().Ambient,
            static value => Visuals.Mutate().Ambient.Ambient = (Vector3)value
        ));
        AmbientGround = Register(new ColorField("Ambient Ground", false,
            static () => (Color4)Visuals.GetAmbient().AmbientGround,
            static value => Visuals.Mutate().Ambient.AmbientGround = (Vector3)value
        ));
        Exposure = Register(new FloatField<Float1Value>("Exposure", FieldWidgetKind.Drag,
            static () => Visuals.GetAmbient().Exposure,
            static value => Visuals.Mutate().Ambient.Exposure = (float)value)
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
            static () => Visuals.GetShadow().ShadowMapSize,
            static value => Visuals.SetShadowSize((int)value)
        ).WithProperties(FieldGetDelay.VeryHigh, FieldLayout.None).WithPlaceholder("No Shadow"));

        ShadowProjectionFields = Register(new FloatGroupField<Float4Value>(
            "Shadow Projection",
            static () =>
            {
                ref readonly var it = ref Visuals.GetShadow();
                return new Float4Value(it.Distance, it.ZPad, it.ConstBias, it.SlopeBias);
            },
            static value =>
            {
                var mutate = Visuals.Mutate();
                mutate.Shadow.Distance = value.X;
                mutate.Shadow.ZPad = value.Y;
                mutate.Shadow.ConstBias = value.Z;
                mutate.Shadow.SlopeBias = value.W;
            })
        .WithProperties(FieldGetDelay.VeryHigh)
        .WithSlider("Distance", 10f, 500f)
        .WithSlider("Z-Padding", 0f, 100f)
        .WithDrag("Const Bias", 0.001f, 0.0001f, 0.01f, "%.4f")
        .WithDrag("Slope Bias", 0.001f, 0.001f, 0.01f, "%.4f"));

        ShadowVisualFields = Register(new FloatGroupField<Float2Value>(
            "Shadow Visual",
            static () =>
            {
                ref readonly var it = ref Visuals.GetShadow();
                return new Float2Value(it.Strength, it.PcfRadius);
            },
            static value =>
            {
                var mutate = Visuals.Mutate();
                mutate.Shadow.Strength = value.X;
                mutate.Shadow.PcfRadius = value.Y;
            })
        .WithProperties(FieldGetDelay.VeryHigh)
        .WithSlider("Strength", 0f, 1f).WithSlider("PcfRadius", 0.5f, 4f));


        // Fog
        FogColorField = Register(new ColorField("FogColor", false,
            static () => (Color4)Visuals.GetFog().Color,
            static value => Visuals.Mutate().Fog.Color = (Vector3)value)
        .WithProperties(FieldGetDelay.VeryHigh));

        FogHeightFields = Register(new FloatGroupField<Float4Value>(
            "Fog Height",
            static () =>
            {
                ref readonly var f = ref Visuals.GetFog();
                return new Float4Value(f.Density, f.BaseHeight, f.HeightFalloff, f.HeightInfluence);
            },
            static value =>
            {
                var mutate = Visuals.Mutate();
                mutate.Fog.Density = value.X;
                mutate.Fog.BaseHeight = value.Y;
                mutate.Fog.HeightFalloff = value.Z;
                mutate.Fog.HeightInfluence = value.W;
            })
        .WithProperties(FieldGetDelay.VeryHigh)
        .WithSlider("Density", 100, 1500, "%.5f").WithSlider("BaseHeight", -1000f, 1000f, "%.3f")
        .WithSlider("Falloff", 0.001f, 10000.0f, "%.3f").WithDrag("Influence", 0.001f, 0f, 1f, "%.3f"));

        FogOpticsFields = Register(new FloatGroupField<Float3Value>(
            "Fog Optics",
            static () =>
            {
                ref readonly var f = ref Visuals.GetFog();
                return new Float3Value(f.Scattering, f.Strength, f.MaxDistance);
            },
            static value =>
            {
                var mutate = Visuals.Mutate();
                mutate.Fog.Scattering = value.X;
                mutate.Fog.Strength = value.Y;
                mutate.Fog.MaxDistance = value.Z;
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

    public override void Bind(VisualEnvironment target)
    {
    }
}