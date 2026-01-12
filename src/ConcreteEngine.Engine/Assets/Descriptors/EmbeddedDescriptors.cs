using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets.Internal;
using ConcreteEngine.Engine.Assets.Loader.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;

namespace ConcreteEngine.Engine.Assets.Descriptors;

internal abstract class EmbeddedRecord : IComparable<EmbeddedRecord>
{
    public required Guid GId { get; init; }

    public string AssetName { get; set; }
    public string EmbeddedName { get; set; }

    public AssetFileSpec[] FileSpec { get; set; }

    public abstract int Index { get; init; }
    public abstract int Priority { get; }
    public abstract AssetKind Kind { get; }
    public abstract Type AssetType { get; }

    public int CompareTo(EmbeddedRecord? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;

        var c = Priority.CompareTo(other.Priority);
        if (c != 0) return c;

        return Index.CompareTo(other.Index);
    }
}

internal sealed class MaterialEmbeddedRecord : EmbeddedRecord
{
    public MaterialImportData Data;
    public MaterialImportProps Props;

    public bool IsAnimated { get; set; }
    public override int Index { get; init; }
    public Dictionary<(int, int), Guid> EmbeddedTextures { get; } = [];

    public override int Priority => AssetPriority.Material;
    public override AssetKind Kind => AssetKind.Material;
    public override Type AssetType => typeof(MaterialTemplate);
}

internal sealed class TextureEmbeddedRecord : EmbeddedRecord
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required MaterialSlotKind SlotKind { get; init; } = MaterialSlotKind.Albedo;
    public required TexturePixelFormat PixelFormat { get; init; }
    public required byte[] PixelData { get; init; } = [];

    public override int Priority => AssetPriority.Texture;
    public override int Index { get; init; }

    public override Type AssetType => typeof(Texture2D);
    public override AssetKind Kind => AssetKind.Texture;
}