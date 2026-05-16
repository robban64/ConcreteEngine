using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Core;

namespace ConcreteEngine.Core.Engine.Assets.Descriptors;

internal interface IEmbeddedAsset
{
    Guid GId { get; }
    AssetKind Kind { get; }
    string Name { get; }
    string EmbeddedName { get; }
    AssetFile? FileSpec { get; }
}

internal sealed class EmbeddedSceneMaterial(string name, int materialIndex, bool isAnimated) : IEmbeddedAsset
{
    public Guid GId { get; } = Guid.NewGuid();
    public AssetKind Kind => AssetKind.Material;
    public string Name { get; } = name;
    public string EmbeddedName { get; set; } = null!;
    public AssetFile? FileSpec { get; set; }

    public readonly int MaterialIndex = materialIndex;
    public readonly bool IsAnimated = isAnimated;

    public MaterialParams Params;

    public readonly List<AssetIndexRef> Textures = new(4);
}

internal sealed class EmbeddedSceneTexture(string name, string embeddedName, int textureIndex) : IEmbeddedAsset
{
    public Guid GId { get; } = Guid.NewGuid();
    public AssetKind Kind => AssetKind.Texture;
    public string EmbeddedName { get; } = embeddedName;
    public string Name { get; } = name;

    public readonly int TextureIndex = textureIndex;

    public AssetFile? FileSpec { get; set; }

    public Size2D Dimensions;

    public MemoryBlockPtr PixelDataBlock = null;

    public TextureUsage SlotKind = TextureUsage.Albedo;
    public TexturePreset Preset = TexturePreset.LinearMipmapRepeat;
    public TexturePixelFormat PixelFormat = TexturePixelFormat.Unknown;

    public bool Discard;
}