using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Assets.Loader.Data;

internal interface IEmbeddedAsset
{
    Guid GId { get; }
    string Name { get; }
    string EmbeddedName { get; }
    AssetFileSpec? FileSpec { get; }
}

internal sealed class EmbeddedSceneMaterial(string name, int materialIndex, bool isAnimated) : IEmbeddedAsset
{
    public Guid GId { get; } = Guid.NewGuid();
    public string Name { get; } = name;
    public string EmbeddedName { get; set; } = null!;
    public AssetFileSpec? FileSpec { get; set; }

    public readonly int MaterialIndex = materialIndex;
    public readonly bool IsAnimated = isAnimated;

    public MaterialParams Params;

    public readonly List<(Guid TextureGId, int TextureIndex)> Textures = new(4);
}

internal sealed class EmbeddedSceneTexture(string name, string embeddedName, int textureIndex) : IEmbeddedAsset
{
    public Guid GId { get; } = Guid.NewGuid();
    public string EmbeddedName { get; } = embeddedName;
    public string Name { get; } = name;

    public readonly int TextureIndex = textureIndex;

    public AssetFileSpec? FileSpec { get; set; }

    public Size2D Dimensions;

    public byte[]? PixelData;

    public TextureUsage SlotKind = TextureUsage.Albedo;
    public TexturePreset Preset = TexturePreset.LinearMipmapRepeat;
    public TexturePixelFormat PixelFormat = TexturePixelFormat.Unknown;

    public bool Discard;
}