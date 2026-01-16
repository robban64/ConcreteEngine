using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Graphics.Gfx.Handles;

namespace ConcreteEngine.Editor.Bridge;

public sealed class AssetProxy(IAsset asset, AssetFileSpec[] fileSpecs)
{
    public readonly IAsset Asset = asset;

    public readonly AssetFileSpec[] FileSpecs = fileSpecs;

    public required IAssetProxyProperty Property;

    public string GIdString { get; } = asset.GId.ToString()[..8];
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

public sealed class MaterialProxyProperty(IMaterial asset) : AssetProxyProperty<IMaterial>(asset)
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