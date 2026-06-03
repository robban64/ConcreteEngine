using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Editor.Inspector.Impl;

internal sealed class InspectMaterialFields : InspectorFields<InspectMaterial>
{
    public readonly ColorField ColorField;
    public readonly FloatField<Float1> SpecularField;
    public readonly FloatField<Float1> ShininessField;
    public readonly FloatField<Float1> UvRepeatField;
    public readonly ComboField BlendCombo;
    public readonly ComboField CullCombo;
    public readonly ComboField DepthCombo;
    public readonly ComboField PolygonCombo;

    protected override FieldLayout DefaultLayout => FieldLayout.Inline;
    protected override FieldGetDelay DefaultDelay => FieldGetDelay.High;


    public InspectMaterialFields() : base(segmentCount: 2)
    {
        ColorField = Register(new ColorField("Color", true));
        SpecularField = Register(new FloatField<Float1>("Specular", FieldWidgetKind.Slider) { Min = 0, Max = 50 });
        ShininessField =
            Register(new FloatField<Float1>("Shininess", FieldWidgetKind.Slider) { Min = 0, Max = 50 });
        UvRepeatField = Register(new FloatField<Float1>("UV Repeat", FieldWidgetKind.Slider));

        BlendCombo = Register(ComboField.MakeFromEnumCache<BlendMode>("Blend Mode"));
        CullCombo = Register(ComboField.MakeFromEnumCache<CullMode>("Cull Mode"));
        DepthCombo = Register(ComboField.MakeFromEnumCache<DepthMode>("Depth Mode"));
        PolygonCombo = Register(ComboField.MakeFromEnumCache<PolygonOffsetLevel>("Polygon Offset"));

        CreateSegment("State Properties", [ColorField, SpecularField, ShininessField, UvRepeatField]);
        CreateSegment("State Value", [BlendCombo, CullCombo, DepthCombo, PolygonCombo]);
    }

    public override void Bind(InspectMaterial target)
    {
        ColorField.Bind(() => target.State.Color,
            value => target.State.Color = (Color4)value
        );
        SpecularField.Bind(
            () => target.State.Specular,
            value => target.State.Specular = (float)value
        );
        ShininessField.Bind(
            () => target.State.Shininess,
            value => target.State.Shininess = (float)value
        );
        UvRepeatField.Bind(
            () => target.State.UvRepeat,
            value => target.State.UvRepeat = (float)value
        );
        BlendCombo.Bind(
            () => (int)target.State.PassFunctions.Blend,
            value => target.State.PassFunctions = target.State.PassFunctions with {Blend = (BlendMode)value.X}
        );
        CullCombo.Bind(
            () => (int)target.State.PassFunctions.Cull,
            value => target.State.PassFunctions = target.State.PassFunctions with { Cull = (CullMode)value.X }
        );
        DepthCombo.Bind(
            () => (int)target.State.PassFunctions.Depth,
            value => target.State.PassFunctions = target.State.PassFunctions with { Depth = (DepthMode)value.X }
        );
        PolygonCombo.Bind(
            () => (int)target.State.PassFunctions.PolygonOffset,
            value => target.State.PassFunctions = target.State.PassFunctions with
            {
                PolygonOffset = (PolygonOffsetLevel)value.X
            }
        );
    }
}

internal sealed class InspectTextureFields : InspectorFields<InspectTexture>
{
    public readonly FloatField<Float1> LodBias;
    public readonly ComboField Preset;
    public readonly ComboField Anisotropy;
    public readonly ComboField Usage;
    public readonly ComboField PixelFormat;

    protected override FieldLayout DefaultLayout => FieldLayout.Inline;
    protected override FieldGetDelay DefaultDelay => FieldGetDelay.High;

    public InspectTextureFields() : base(segmentCount: 2)
    {
        LodBias = Register(new FloatField<Float1>("Lod Level", FieldWidgetKind.Input) { Format = "%.3f" });
        Preset = Register(ComboField.MakeFromEnumCache<TexturePreset>("Preset").WithStartAt(1));
        Anisotropy = Register(ComboField.MakeFromEnumCache<AnisotropyLevel>("Anisotropy"));
        Usage = Register(ComboField.MakeFromEnumCache<TextureUsage>("Usage").WithPlaceholder("None"));
        PixelFormat = Register(ComboField.MakeFromEnumCache<TexturePixelFormat>("Format").WithPlaceholder("None")
            .WithStartAt(1));

        CreateSegment("Texture State", [Usage]);
        CreateSegment("Gpu State", [LodBias, Preset, Anisotropy, PixelFormat]);
    }

    public override void Bind(InspectTexture target)
    {
        LodBias.Bind(
            () => target.GpuState.LodBias,
            (value) => target.GpuState.LodBias = (float)value
        );
        Preset.Bind(
            () => (int)target.GpuState.Preset,
            value => target.GpuState.Preset = (TexturePreset)value.X
        );
        Anisotropy.Bind(
            () => (int)target.GpuState.Anisotropy,
            value => target.GpuState.Anisotropy = (AnisotropyLevel)value.X
        );
        Usage.Bind(
            () => (int)target.Asset.Usage,
            value => target.Asset.Usage = (TextureUsage)value.X
        );
        PixelFormat.Bind(
            () => (int)target.GpuState.PixelFormat,
            value => target.GpuState.PixelFormat = (TexturePixelFormat)value.X
        );
    }
}