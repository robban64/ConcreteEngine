using ConcreteEngine.Engine.Metadata;

namespace ConcreteEngine.Editor.Store.Resources;

public sealed class EditorAssetResource : EditorResource, IComparable<EditorAssetResource>
{
    public required int ResourceId { get; set; }
    public required string ResourceName { get; set; }
    public required AssetKind Kind { get; set; }
    public required bool IsCoreAsset { get; init; }
    public required string SpecialName { get; set; }
    public required string SpecialValue { get; set; }
    public required bool HasActions { get; init; }

    public int CompareTo(EditorAssetResource? other)
    {
        if (ReferenceEquals(this, other)) return 0;

        if (other is null) return 1;

        var c = ((byte)Kind).CompareTo((byte)other.Kind);
        if (c != 0) return c;
        return ResourceId.CompareTo(other.ResourceId);
    }
}

public sealed class EditorFileAssetModel
{
    public required int AssetFileId { get; init; }
    public required string RelativePath { get; init; }
    public required long SizeInBytes { get; init; }
    public string? ContentHash { get; init; }
}