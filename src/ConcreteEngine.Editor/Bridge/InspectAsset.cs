using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Editor.Bridge;

internal abstract class InspectAsset(AssetFileSpec[] fileSpecs)
{
    public abstract AssetObject Asset { get; }
    public readonly AssetFileSpec[] FileSpecs = fileSpecs;

    public AssetId Id => Asset.Id;
    public AssetKind Kind => Asset.Kind;
    public string Name => Asset.Name;

    internal void Rename(string newName)
    {
        Asset.SetName(newName);
    }

    internal abstract char GetIcon();
}

internal class InspectMaterial : InspectAsset
{
    public override Material Asset { get; }
    public GfxPassFunctions PassFunctions => Asset.Pipeline.PassFunctions;
    public GfxPassState PassState => Asset.Pipeline.PassState;

    internal override char GetIcon() => AssetIcons.GetMaterialIcon(Asset);

    public readonly ColorField ColorField;
    public readonly FloatField<Float1Value> SpecularField;
    public readonly FloatField<Float1Value> ShininessField;
    public readonly FloatField<Float1Value> UvRepeatField;

    public readonly ComboField BlendCombo;
    public readonly ComboField CullCombo;
    public readonly ComboField DepthCombo;
    public readonly ComboField PolygonCombo;

    public InspectMaterial(Material asset, AssetFileSpec[] fileSpecs) : base(fileSpecs)
    {
        Asset = asset;

        ColorField = new ColorField("Color", true,
            () => Asset.Color,
            value => Asset.Color = (Color4)value
        ) { Delay = FieldGetDelay.VeryHigh };

        SpecularField = new FloatField<Float1Value>("Specular", FieldWidgetKind.Slider,
            () => Asset.Specular,
            value => Asset.Specular = (float)value
        ) { Delay = FieldGetDelay.VeryHigh, Min = 0, Max = 50, Layout = FieldLabelLayout.Inline };

        ShininessField = new FloatField<Float1Value>("Shininess", FieldWidgetKind.Slider,
            () => Asset.Shininess,
            value => Asset.Shininess = (float)value
        ) { Delay = FieldGetDelay.VeryHigh, Min = 0, Max = 50, Layout = FieldLabelLayout.Inline };

        UvRepeatField = new FloatField<Float1Value>("UV Repeat", FieldWidgetKind.Input,
            () => Asset.UvRepeat,
            value => Asset.UvRepeat = (float)value
        ) { Delay = FieldGetDelay.VeryHigh, Layout = FieldLabelLayout.Inline };

        BlendCombo = ComboField.MakeFromEnumCache<BlendMode>("Blend Mode",
            () => (int)PassFunctions.Blend,
            value => Asset.SetPassFunction(PassFunctions with { Blend = (BlendMode)value.X })
        ).WithStartAt(1);

        CullCombo = ComboField.MakeFromEnumCache<CullMode>("Cull Mode",
            () => (int)PassFunctions.Cull,
            value => Asset.SetPassFunction(PassFunctions with { Cull = (CullMode)value.X })
        ).WithStartAt(1);

        DepthCombo = ComboField.MakeFromEnumCache<DepthMode>("Depth Mode",
            () => (int)PassFunctions.Depth,
            value => Asset.SetPassFunction(PassFunctions with { Depth = (DepthMode)value.X })
        ).WithStartAt(1);

        PolygonCombo = ComboField.MakeFromEnumCache<PolygonOffsetLevel>("Polygon Offset",
            () => (int)PassFunctions.PolygonOffset,
            value => Asset.SetPassFunction(PassFunctions with { PolygonOffset = (PolygonOffsetLevel)value.X })
        ).WithStartAt(1);
    }
}

internal class InspectModel(Model asset, AssetFileSpec[] fileSpecs) : InspectAsset(fileSpecs)
{
    public override Model Asset { get; } = asset;

    internal override char GetIcon() => AssetIcons.GetModelIcon(Asset);
}

internal class InspectTexture : InspectAsset
{
    public override Texture Asset { get; }
    internal override char GetIcon() => IconNames.Image;
    public readonly FloatField<Float1Value> LodBias;
    public readonly ComboField Preset;
    public readonly ComboField Anisotropy;
    public readonly ComboField Usage;
    public readonly ComboField PixelFormat;

    public InspectTexture(Texture asset, AssetFileSpec[] fileSpecs) : base(fileSpecs)
    {
        Asset = asset;

        LodBias = new FloatField<Float1Value>("Lod Level", FieldWidgetKind.Input,
            () => Asset.LodBias,
            (value) => Asset.LodBias = (float)value
        ) { Format = "%.3f", Delay = FieldGetDelay.VeryHigh };

        Preset = ComboField.MakeFromEnumCache<TexturePreset>("Preset",
            () => (int)Asset.Preset,
            value => Asset.Preset = (TexturePreset)value.X
        ).WithStartAt(1);

        Anisotropy = ComboField.MakeFromEnumCache<AnisotropyLevel>("Anisotropy",
            () => (int)Asset.Anisotropy,
            value => Asset.Anisotropy = (AnisotropyLevel)value.X
        );

        Usage = ComboField.MakeFromEnumCache<TextureUsage>("Usage",
            () => (int)Asset.Usage,
            value => Asset.Usage = (TextureUsage)value.X
        ).WithPlaceholder("None");

        PixelFormat = ComboField.MakeFromEnumCache<TexturePixelFormat>("Format",
            () => (int)Asset.PixelFormat,
            value => Asset.PixelFormat = (TexturePixelFormat)value.X
        ).WithPlaceholder("None").WithStartAt(1);
    }
}

internal class InspectShader(Shader asset, AssetFileSpec[] fileSpecs) : InspectAsset(fileSpecs)
{
    public override Shader Asset { get; } = asset;
    internal override char GetIcon() => AssetIcons.GetShaderIcon();
}

internal static class AssetIcons
{
    public static char GetTextureIcon() => IconNames.Image;
    public static char GetModelIcon(Model model) => model.Info.MeshCount > 1 ? IconNames.Boxes : IconNames.Box;

    public static char GetMaterialIcon(Material material) =>
        material.Transparency ? IconNames.CircleDashed : IconNames.Circle;

    public static char GetShaderIcon() => IconNames.Code;
}