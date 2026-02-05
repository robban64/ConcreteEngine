using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Editor.Controller.Proxy;

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

public sealed class MaterialProxyProperty(IMaterial asset, in MaterialParams param, MaterialPipeline pipeline)
    : AssetProxyProperty<IMaterial>(asset)
{
    public MaterialParams Params = param;
    public MaterialPipeline Pipeline = pipeline;

    public required IMaterial? TemplateMaterial;
    public required IShader Shader;
    public required ITexture?[] Textures;
    public required TextureSource[] Bindings;

    public required Action<MaterialProxyProperty> CommitDel;
    public required Action<MaterialProxyProperty> FetchDel;
    public void Commit() => CommitDel(this);
    public void Fetch() => FetchDel(this);
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

    public sealed class MeshPart(string name, MeshId gfxId, MeshInfo info)
    {
        public string Name = name;
        public MeshId GfxId = gfxId;
        public MeshInfo Info = info;
    }

    public sealed class Clip(string name, int trackCount, float duration, float ticksPerSecond)
    {
        public string Name = name;
        public int TrackCount = trackCount;
        public float Duration = duration;
        public float TicksPerSecond = ticksPerSecond;
    }
}