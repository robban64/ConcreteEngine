using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Graphics.Gfx.Contracts;

namespace ConcreteEngine.Editor.Lib;

internal abstract class InspectAsset()
{
    public abstract AssetObject Asset { get; }

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

    public InspectMaterial(Material asset) : base()
    {
        Asset = asset;
        InspectorFieldProvider.Instance.MaterialFields.Bind(this);
    }
}

internal class InspectModel(Model asset) : InspectAsset
{
    public override Model Asset { get; } = asset;

    internal override Icons GetIcon() => AssetIcons.GetModelIcon(Asset);
}

internal class InspectTexture : InspectAsset
{
    public override Texture Asset { get; }
    internal override Icons GetIcon() => Icons.Image;

    public InspectTexture(Texture asset) : base()
    {
        Asset = asset;
        InspectorFieldProvider.Instance.TextureFields.Bind(this);
        
    }
}

internal class InspectShader(Shader asset) : InspectAsset
{
    public override Shader Asset { get; } = asset;
    internal override Icons GetIcon() => AssetIcons.ShaderIcon;
}

internal static class AssetIcons
{
    public const Icons ModelIcon = Icons.Box;
    public const Icons TextureIcon = Icons.Image;
    public const Icons ShaderIcon = Icons.Code;
    public const Icons MaterialIcon = Icons.Circle;

    public const Icons ModelFileIcon = Icons.FileBox;
    public const Icons TextureFileIcon = Icons.FileImage;
    public const Icons ShaderFileIcon = Icons.FileCode;

    public static Icons GetModelIcon(Model model) => model.Info.MeshCount > 1 ? Icons.Boxes : Icons.Box;

    public static Icons GetMaterialIcon(Material material) => material.Transparency ? Icons.CircleDashed : Icons.Circle;
    
}

