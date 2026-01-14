using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Editor.Bridge;

public sealed class AssetProxy(IAsset asset, AssetFileSpec[] fileSpecs)
{
    public readonly IAsset Asset = asset;

    public readonly AssetFileSpec[] FileSpecs = fileSpecs;

    public required IAssetProxyProperty Property;

    public string GIdString { get; } = asset.GId.ToString()[..8];
}

public interface IAssetProxyProperty { }

public abstract class AssetProxyProperty<T> : IAssetProxyProperty where T : class, IAsset { }

public sealed class TextureProxyProperty : AssetProxyProperty<ITexture> { }

public sealed class ShaderProxyProperty : AssetProxyProperty<IShader> { }

public sealed class ModelProxyProperty : AssetProxyProperty<IModel> { }

public sealed class MaterialProxyProperty : AssetProxyProperty<IMaterial>
{
    public required IMaterial? TemplateMaterial;
    public required IShader Shader;
    public required ITexture?[] Textures;
    public required TextureSource[] Bindings;
    
    public sealed class Slot
    {
        public string Name { get; }
        public ITexture Texture;
        public TextureSource Source;
    }
}
