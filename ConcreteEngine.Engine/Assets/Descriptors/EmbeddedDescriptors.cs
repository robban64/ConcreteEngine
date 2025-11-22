using System.Collections.ObjectModel;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Assets.Descriptors;


internal interface IAssetEmbeddedDescriptor
{
    string Name { get; set; }
    string AssetName { get; set; }
    AssetKind Kind { get; }
    Type AssetType { get; }
    AssetFileSpec[] FileSpec { get; }

}

internal sealed class ModelMaterialEmbeddedDescriptor : IAssetEmbeddedDescriptor
{
    public string Name { get; set; }
    public string AssetName { get; set; }

    public Color4 Color { get; set; }

    public AssetFileSpec[] FileSpec { get; set; }

    public Dictionary<string, TextureEmbeddedDescriptor> EmbeddedTextures { get; } = [];
    
    public AssetKind Kind => AssetKind.EmbeddedMaterial;
    public Type AssetType => typeof(MaterialTemplate);

}

internal sealed class TextureEmbeddedDescriptor : IAssetEmbeddedDescriptor
{
    public required string Name { get; set; }
    public string AssetName { get; set; }

    public required int Width { get; set; }
    public required int Height { get; set; }
    public required TextureSlotKind SlotKind { get; init; }
    public required TexturePixelFormat PixelFormat { get; init; }
    public required byte[] PixelData { get; set; } = Array.Empty<byte>();

    public AssetFileSpec[] FileSpec { get; set; } = Array.Empty<AssetFileSpec>();


    public Type AssetType => typeof(Texture2D);

    public AssetKind Kind => AssetKind.Texture2D;

}