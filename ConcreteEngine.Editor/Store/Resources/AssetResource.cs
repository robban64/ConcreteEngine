using ConcreteEngine.Editor.Definitions;

namespace ConcreteEngine.Editor.Store.Resources;

public sealed class EditorAssetResource : EditorResource
{
    public required int ResourceId { get; set; }
    public required string ResourceName { get; set; }
    public required EditorAssetCategory AssetCategory { get; set; }
    public required bool IsCoreAsset { get; init; }
    public required int Generation { get; set; }
    public required string SpecialName { get; set; }
    public required string SpecialValue { get; set; }
    public required bool HasActions { get; init; }
}

public sealed class EditorFileAssetModel
{
    public required int AssetFileId { get; init; }
    public required string RelativePath { get; init; }
    public required long SizeInBytes { get; init; }
    public string? ContentHash { get; init; }

}