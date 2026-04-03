using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Editor.Lib.Impl;

internal sealed class InspectMaterialFields : InspectorFields<InspectMaterial>
{
    public readonly ColorField ColorField;
    public readonly FloatField<Float1Value> SpecularField;
    public readonly FloatField<Float1Value> ShininessField;
    public readonly FloatField<Float1Value> UvRepeatField;
    public readonly ComboField BlendCombo;
    public readonly ComboField CullCombo;
    public readonly ComboField DepthCombo;
    public readonly ComboField PolygonCombo;

    protected override FieldLayout DefaultLayout => FieldLayout.Inline;
    protected override FieldGetDelay DefaultDelay => FieldGetDelay.High;


    public InspectMaterialFields() : base(segmentCount: 2)
    {
        ColorField = Register(new ColorField("Color", true));
        SpecularField = Register(new FloatField<Float1Value>("Specular", FieldWidgetKind.Slider) { Min = 0, Max = 50 });
        ShininessField =
            Register(new FloatField<Float1Value>("Shininess", FieldWidgetKind.Slider) { Min = 0, Max = 50 });
        UvRepeatField = Register(new FloatField<Float1Value>("UV Repeat", FieldWidgetKind.Slider));

        BlendCombo = Register(ComboField.MakeFromEnumCache<BlendMode>("Blend Mode"));
        CullCombo = Register(ComboField.MakeFromEnumCache<CullMode>("Cull Mode"));
        DepthCombo = Register(ComboField.MakeFromEnumCache<DepthMode>("Depth Mode"));
        PolygonCombo = Register(ComboField.MakeFromEnumCache<PolygonOffsetLevel>("Polygon Offset"));

        CreateSegment("State Properties", [ColorField, SpecularField, ShininessField, UvRepeatField]);
        CreateSegment("State Value", [BlendCombo, CullCombo, DepthCombo, PolygonCombo]);
    }

    public override void Bind(InspectMaterial target)
    {
        ColorField.Bind(() => target.Asset.Color,
            value => target.Asset.Color = (Color4)value
        );
        SpecularField.Bind(
            () => target.Asset.Specular,
            value => target.Asset.Specular = (float)value
        );
        ShininessField.Bind(
            () => target.Asset.Shininess,
            value => target.Asset.Shininess = (float)value
        );
        UvRepeatField.Bind(
            () => target.Asset.UvRepeat,
            value => target.Asset.UvRepeat = (float)value
        );
        BlendCombo.Bind(
            () => (int)target.PassFunctions.Blend,
            value => target.Asset.SetPassFunction(target.PassFunctions with { Blend = (BlendMode)value.X })
        );
        CullCombo.Bind(
            () => (int)target.PassFunctions.Cull,
            value => target.Asset.SetPassFunction(target.PassFunctions with { Cull = (CullMode)value.X })
        );
        DepthCombo.Bind(
            () => (int)target.PassFunctions.Depth,
            value => target.Asset.SetPassFunction(target.PassFunctions with { Depth = (DepthMode)value.X })
        );
        DepthCombo.Bind(
            () => (int)target.PassFunctions.PolygonOffset,
            value => target.Asset.SetPassFunction(target.PassFunctions with
            {
                PolygonOffset = (PolygonOffsetLevel)value.X
            })
        );
    }
}

internal sealed class InspectTextureFields : InspectorFields<InspectTexture>
{
    public readonly FloatField<Float1Value> LodBias;
    public readonly ComboField Preset;
    public readonly ComboField Anisotropy;
    public readonly ComboField Usage;
    public readonly ComboField PixelFormat;

    protected override FieldLayout DefaultLayout => FieldLayout.Inline;
    protected override FieldGetDelay DefaultDelay => FieldGetDelay.High;

    public InspectTextureFields() : base(segmentCount: 1)
    {
        LodBias = Register(new FloatField<Float1Value>("Lod Level", FieldWidgetKind.Input) { Format = "%.3f" });
        Preset = Register(ComboField.MakeFromEnumCache<TexturePreset>("Preset").WithStartAt(1));
        Anisotropy = Register(ComboField.MakeFromEnumCache<AnisotropyLevel>("Anisotropy"));
        Usage = Register(ComboField.MakeFromEnumCache<TextureUsage>("Usage").WithPlaceholder("None"));
        PixelFormat = Register(ComboField.MakeFromEnumCache<TexturePixelFormat>("Format").WithPlaceholder("None")
            .WithStartAt(1));

        CreateSegment("Texture State", [LodBias, Preset, Anisotropy, Usage, PixelFormat]);
    }

    public override void Bind(InspectTexture target)
    {
        LodBias.Bind(
            () => target.Asset.LodBias,
            (value) => target.Asset.LodBias = (float)value
        );
        Preset.Bind(
            () => (int)target.Asset.Preset,
            value => target.Asset.Preset = (TexturePreset)value.X
        );
        Anisotropy.Bind(
            () => (int)target.Asset.Anisotropy,
            value => target.Asset.Anisotropy = (AnisotropyLevel)value.X
        );
        Usage.Bind(
            () => (int)target.Asset.Usage,
            value => target.Asset.Usage = (TextureUsage)value.X
        );
        PixelFormat.Bind(
            () => (int)target.Asset.PixelFormat,
            value => target.Asset.PixelFormat = (TexturePixelFormat)value.X
        );
    }
}