using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Theme;
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

    internal abstract Icons GetIcon();
}

internal class InspectMaterial : InspectAsset
{
    public override Material Asset { get; }
    public GfxPassFunctions PassFunctions => Asset.Pipeline.PassFunctions;
    public GfxPassState PassState => Asset.Pipeline.PassState;

    internal override Icons GetIcon() => AssetIcons.GetMaterialIcon(Asset);

    public InspectMaterial(Material asset, AssetFileSpec[] fileSpecs) : base(fileSpecs)
    {
        Asset = asset;
        InspectorFieldProvider.Instance.MaterialFields.Bind(this);
    }
}

internal class InspectModel(Model asset, AssetFileSpec[] fileSpecs) : InspectAsset(fileSpecs)
{
    public override Model Asset { get; } = asset;

    internal override Icons GetIcon() => AssetIcons.GetModelIcon(Asset);
}

internal class InspectTexture : InspectAsset
{
    public override Texture Asset { get; }
    internal override Icons GetIcon() => Icons.Image;

    public InspectTexture(Texture asset, AssetFileSpec[] fileSpecs) : base(fileSpecs)
    {
        Asset = asset;
        InspectorFieldProvider.Instance.TextureFields.Bind(this);
        
    }
}

internal class InspectShader(Shader asset, AssetFileSpec[] fileSpecs) : InspectAsset(fileSpecs)
{
    public override Shader Asset { get; } = asset;
    internal override Icons GetIcon() => AssetIcons.ShaderIcon;
}

internal static class AssetIcons
{
    public const Icons ModelIcon = Icons.Box;
    public const Icons TextureIcon = Icons.Image;
    public const Icons ShaderIcon = Icons.Code;

    public static Icons GetModelIcon(Model model) => model.Info.MeshCount > 1 ? Icons.Boxes : Icons.Box;

    public static Icons GetMaterialIcon(Material material) => material.Transparency ? Icons.CircleDashed : Icons.Circle;
}

