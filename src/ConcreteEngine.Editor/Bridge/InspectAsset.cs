using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Utils;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Editor.Bridge;

public abstract class InspectAsset(AssetFileSpec[] fileSpecs)
{
    public abstract AssetObject Asset { get; }
    public readonly AssetFileSpec[] FileSpecs = fileSpecs;
    
    public AssetId Id => Asset.Id;
    public AssetKind Kind => Asset.Kind;
    internal abstract char GetIcon();
}

internal class InspectMaterial : InspectAsset
{
    public override Material Asset { get; }
    public GfxPassFunctions PassFunctions => Asset.Pipeline.PassFunctions;
    public GfxPassState PassState => Asset.Pipeline.PassState;

    internal override char GetIcon() => AssetIcons.GetMaterialIcon(Asset);

    public readonly ColorInputField ColorField;
    public readonly FloatSliderField<Float1Value> SpecularField;
    public readonly FloatSliderField<Float1Value> ShininessField;
    public readonly FloatInputValueField<Float1Value> UvRepeatField;

    public readonly ComboField BlendCombo;
    public readonly ComboField CullCombo;
    public readonly ComboField DepthCombo;
    public readonly ComboField PolygonCombo;

    public InspectMaterial(Material asset, AssetFileSpec[] fileSpecs) : base(fileSpecs)
    {
        Asset = asset;

        ColorField = new ColorInputField("Color", true,
            () => Asset.Color,
            value => Asset.Color = value
        ) { Delay = PropertyGetDelay.VeryHigh };

        SpecularField = new FloatSliderField<Float1Value>("Specular", 0, 50,
            () => Asset.Specular,
            value => Asset.Specular = (float)value
        ) { Delay = PropertyGetDelay.VeryHigh };

        ShininessField = new FloatSliderField<Float1Value>("Shininess", 0, 50,
            () => Asset.Shininess,
            value => Asset.Shininess = (float)value
        ) { Delay = PropertyGetDelay.VeryHigh };

        UvRepeatField = new FloatInputValueField<Float1Value>("UV Repeat",
            () => Asset.UvRepeat,
            value => Asset.UvRepeat = (float)value
        ) { Delay = PropertyGetDelay.VeryHigh };

        BlendCombo = ComboField.MakeFromEnumCache<BlendMode>("Blend Mode", "Select",
            () => (int)PassFunctions.Blend,
            value => Asset.SetPassFunction(PassFunctions with { Blend = (BlendMode)value })
        );
        BlendCombo.Delay = PropertyGetDelay.VeryHigh;

        CullCombo = ComboField.MakeFromEnumCache<CullMode>("Cull Mode", "Select",
            () => (int)PassFunctions.Cull,
            value => Asset.SetPassFunction(PassFunctions with { Cull = (CullMode)value })
        );
        CullCombo.Delay = PropertyGetDelay.VeryHigh;

        DepthCombo = ComboField.MakeFromEnumCache<DepthMode>("Depth Mode", "Select",
            () => (int)PassFunctions.Depth,
            value => Asset.SetPassFunction(PassFunctions with { Depth = (DepthMode)value })
        );
        DepthCombo.Delay = PropertyGetDelay.VeryHigh;

        PolygonCombo = ComboField.MakeFromEnumCache<CullMode>("Polygon Offset", "Select",
            () => (int)PassFunctions.PolygonOffset,
            value => Asset.SetPassFunction(PassFunctions with { PolygonOffset = (PolygonOffsetLevel)value })
        );
        PolygonCombo.Delay = PropertyGetDelay.VeryHigh;
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

    public readonly FloatInputValueField<Float1Value> LodBias;
    public readonly ComboField Preset;
    public readonly ComboField Anisotropy;
    public readonly ComboField Usage;
    public readonly ComboField PixelFormat;

    public InspectTexture(Texture asset, AssetFileSpec[] fileSpecs) : base(fileSpecs)
    {
        Asset = asset;
        
        LodBias = new FloatInputValueField<Float1Value>("Lod Level",
            () => Asset.LodBias,
            (value) => Asset.LodBias = (float)value
        ) {Format = "%.3", Delay= PropertyGetDelay.VeryHigh};

        Preset = ComboField.MakeFromEnumCache<TexturePreset>("Preset", "Select",
            () => (int)Asset.Preset,
            value => Asset.Preset = (TexturePreset)value
        );

        Anisotropy = ComboField.MakeFromEnumCache<AnisotropyLevel>("Anisotropy", "Select",
            () => (int)Asset.Anisotropy,
            value => Asset.Anisotropy = (AnisotropyLevel)value
        );

        Usage = ComboField.MakeFromEnumCache<TextureUsage>("Usage", "Select",
            () => (int)Asset.Usage,
            value => Asset.Usage = (TextureUsage)value
        );

        PixelFormat = ComboField.MakeFromEnumCache<TexturePixelFormat>("Format", "Select",
            () => (int)Asset.PixelFormat,
            value => Asset.PixelFormat = (TexturePixelFormat)value
        );
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
    public static char GetModelIcon(Model model) => model.Info.MeshCount > 1 ? IconNames.Boxes :  IconNames.Box;
    public static char GetMaterialIcon(Material material) => material.Transparency ? IconNames.CircleDashed :  IconNames.Circle;
    public static char GetShaderIcon() => IconNames.Code;

}


/*

 [MethodImpl(MethodImplOptions.AggressiveInlining)]
 internal void Draw(in FrameContext ctx)
 {
     var sw = ctx.Writer;
     var items = Inspector.Items;
     foreach (var item in items)
     {
         ImGui.Spacing();
         ImGui.TextUnformatted(ref sw.Write(item.FieldName));
         if (item.Info.Length > 0)
         {
             ImGui.SameLine();
             ImGui.TextUnformatted(ref sw.Start('[').Append(item.Info).Append(']').End());
         }

         ImGui.Separator();
         item.Draw(in ctx);
     }
     Inspector.EndFrame();
 }
}*/