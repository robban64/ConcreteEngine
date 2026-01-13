using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;

namespace ConcreteEngine.Editor.Bridge;

public interface IAssetProxy
{
    IAsset Asset { get; }
}

public abstract class AssetProxy<T> : IAssetProxy where T : class, IAsset
{
    IAsset IAssetProxy.Asset => Asset;

    public required T Asset { get; init; }
    public required AssetProxyProperty<T> Property { get; init; }
    public required AssetFileSpec[] FileSpecs { get; init; }

    public abstract string GIdString { get; }
}

public abstract class AssetProxyProperty<T> where T : class, IAsset
{
}

public sealed class TextureProxyProperty : AssetProxyProperty<ITexture>
{
}

public sealed class MaterialProxyProperty : AssetProxyProperty<IMaterial>
{
    public AssetId TemplateId { get; init; }
    public AssetId AssetShader { get; init; }
    public MaterialTextureSlot[] TextureSlots { get; init; }
}

public sealed class ShaderProxyProperty : AssetProxyProperty<IShader>
{
}

public sealed class ModelProxyProperty : AssetProxyProperty<IModel>
{
}