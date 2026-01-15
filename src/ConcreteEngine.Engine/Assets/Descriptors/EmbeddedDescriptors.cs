using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer.Material;
using ConcreteEngine.Engine.Assets.Internal;
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
    public MaterialParams Data = new();

    public bool IsAnimated { get; set; }
    public override int Index { get; init; }
    public Dictionary<(int, int), Guid> EmbeddedTextures { get; } = [];

    public override int Priority => AssetPriority.Material;
    public override AssetKind Kind => AssetKind.Material;
    public override Type AssetType => typeof(Material);
}

internal sealed class TextureEmbeddedRecord : EmbeddedRecord
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required TextureUsage SlotKind { get; init; } = TextureUsage.Albedo;

    public TexturePreset Preset { get; init; } = TexturePreset.LinearClamp;
    public TextureKind TextureKind { get; init; } = TextureKind.Texture2D;
    public TexturePixelFormat PixelFormat { get; init; } = TexturePixelFormat.SrgbAlpha;
    public TextureAnisotropyProfile Anisotropy { get; init; } = TextureAnisotropyProfile.X4;

    public required byte[] PixelData { get; init; } = [];

    public override int Priority => AssetPriority.Texture;
    public override int Index { get; init; }

    public override Type AssetType => typeof(Texture);
    public override AssetKind Kind => AssetKind.Texture;
}