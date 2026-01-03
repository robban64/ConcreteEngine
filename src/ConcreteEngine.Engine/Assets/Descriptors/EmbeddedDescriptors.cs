using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Assets.Descriptors;

internal interface IAssetEmbeddedDescriptor : IComparable<IAssetEmbeddedDescriptor>
{
    Guid GId { get; }
    string EmbeddedName { get; }
    string AssetName { get; set; }
    AssetKind Kind { get; }
    Type AssetType { get; }
    AssetFileSpec[] FileSpec { get; }
    
    int Order { get; }
}

internal sealed class MaterialEmbeddedDescriptor : IAssetEmbeddedDescriptor
{
    public MaterialImportParams Params;

    public required Guid GId { get; init; }
    public string EmbeddedName { get; set; } = null!;
    public string AssetName { get; set; } = null!;
    public bool IsAnimated { get; set; }
    public int MaterialIndex { get; set; } = -1;
    public AssetFileSpec[] FileSpec { get; set; } = [];
    public Dictionary<(int, int), Guid> EmbeddedTextures { get; } = [];

    public AssetKind Kind => AssetKind.MaterialTemplate;
    public Type AssetType => typeof(MaterialTemplate);
    
    
    public int Order => MaterialIndex;
    
    public int CompareTo(IAssetEmbeddedDescriptor? other)
    {
        if (ReferenceEquals(this, other)) return 0;

        if (other is null) return 1;

        var c = ((byte)Kind).CompareTo((byte)other.Kind);
        if (c != 0) return c;
        return Order.CompareTo(other.Order);
    }
}

internal sealed class TextureEmbeddedDescriptor : IAssetEmbeddedDescriptor
{
    public required Guid GId { get; init; }
    public required string EmbeddedName { get; init; }
    public string AssetName { get; set; } = null!;
    public required int Index { get; set; } = -1;

    public required int Width { get; init; }
    public required int Height { get; init; }
    public required TextureSlotKind SlotKind { get; init; } = TextureSlotKind.Albedo;
    public required TexturePixelFormat PixelFormat { get; init; }
    public required byte[] PixelData { get; init; } = [];

    public required AssetFileSpec[] FileSpec { get; init; } = [];
    
    public Type AssetType => typeof(Texture2D);

    public AssetKind Kind => AssetKind.Texture2D;
    
    public int Order => Index;
    
    public int CompareTo(IAssetEmbeddedDescriptor? other)
    {
        if (ReferenceEquals(this, other)) return 0;

        if (other is null) return 1;

        var c = ((byte)Kind).CompareTo((byte)other.Kind);
        if (c != 0) return c;
        return Order.CompareTo(other.Order);
    }

}