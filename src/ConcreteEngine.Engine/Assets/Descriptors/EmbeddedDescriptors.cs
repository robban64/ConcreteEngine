using ConcreteEngine.Core.Specs.Graphics;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Textures;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Renderer.Definitions;

namespace ConcreteEngine.Engine.Assets.Descriptors;

internal abstract class EmbeddedRecord : IComparable<EmbeddedRecord>
{
    public Guid GId { get; init; } = Guid.NewGuid();

    public string AssetName { get; set; }
    public string EmbeddedName { get; set; }

    public AssetFileSpec[] FileSpec { get; set; }

    public abstract int Index { get; init; }

    public abstract AssetKind Kind { get; }
    public abstract Type AssetType { get; }

    public int CompareTo(EmbeddedRecord? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

        var c = ((byte)Kind).CompareTo((byte)other.Kind);
        if (c != 0) return c;

        return Index.CompareTo(other.Index);
    }
}

internal sealed class MaterialEmbeddedRecord : EmbeddedRecord
{
    public MaterialImportParams Params;

    public bool IsAnimated { get; set; }
    public override int Index { get; init; } = -1;
    public Dictionary<(int, int), Guid> EmbeddedTextures { get; } = [];

    public override AssetKind Kind => AssetKind.MaterialTemplate;
    public override Type AssetType => typeof(MaterialTemplate);
}

internal sealed class TextureEmbeddedRecord : EmbeddedRecord
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required TextureSlotKind SlotKind { get; init; } = TextureSlotKind.Albedo;
    public required TexturePixelFormat PixelFormat { get; init; }
    public required byte[] PixelData { get; init; } = [];

    public override int Index { get; init; }

    public override Type AssetType => typeof(Texture2D);
    public override AssetKind Kind => AssetKind.Texture2D;
}