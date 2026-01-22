using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Editor.Bridge;

public sealed class AssetStoreProxy(AssetController assetController)
{
    public AssetKind ToggledKind;
    public ReadOnlySpan<IAsset> GetAssetSpan(AssetKind kind) => assetController.GetAssetSpan(ToggledKind);
}

public sealed class AssetObjectProxy(IAsset asset, AssetFileSpec[] fileSpecs)
{
    public readonly IAsset Asset = asset;

    public readonly AssetFileSpec[] FileSpecs = fileSpecs;

    public required IAssetProxyProperty Property;

    //public string GIdString { get; } = asset.GId.ToString();
}

public interface IAssetProxyProperty
{
    Type AssetType { get; }
}

public abstract class AssetProxyProperty<T>(T asset) : IAssetProxyProperty where T : class, IAsset
{
    public readonly T Asset = asset;
    public Type AssetType => typeof(T);
}

public sealed class TextureProxyProperty(ITexture asset) : AssetProxyProperty<ITexture>(asset)
{
    public float LodLevel = asset.LodBias;
    public TexturePreset Preset = asset.Preset;
    public AnisotropyLevel Anisotropy = asset.Anisotropy;
    public TextureUsage Usage = asset.Usage;
    public TexturePixelFormat PixelFormat = asset.PixelFormat;
}

public sealed class ShaderProxyProperty(IShader asset) : AssetProxyProperty<IShader>(asset)
{
}

public sealed class ModelProxyProperty(IModel asset) : AssetProxyProperty<IModel>(asset)
{
    public required MeshPart[] Meshes;
    public required Clip[] Clips;

    public required int BoneCount;

    public sealed class MeshPart(string name, MeshId gfxId, MeshSpec spec)
    {
        public string Name = name;
        public MeshId GfxId = gfxId;
        public MeshSpec Spec = spec;
    }

    public sealed class Clip(string name, int trackCount, float duration, float ticksPerSecond)
    {
        public string Name = name;
        public int TrackCount = trackCount;
        public float Duration = duration;
        public float TicksPerSecond = ticksPerSecond;
    }
}

public sealed class MaterialProxyProperty(IMaterial asset, in MaterialParams param, MaterialPipeline pipeline)
    : AssetProxyProperty<IMaterial>(asset)
{
    public MaterialParams Params = param;
    public MaterialPipeline Pipeline = pipeline;

    public required IMaterial? TemplateMaterial;
    public required IShader Shader;
    public required ITexture?[] Textures;
    public required TextureSource[] Bindings;
}