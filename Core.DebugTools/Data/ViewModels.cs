using System.Numerics;
using Core.DebugTools.Definitions;

namespace Core.DebugTools.Data;

public sealed class AssetStoreViewModel
{
    public EditorAssetSelection TypeSelection { get; set; }
    public List<AssetObjectViewModel> AssetObjects { get; } = new(16);
    public List<AssetObjectFileViewModel> AssetFileObjects { get; } = new(4);

    public void ResetState(bool clearTypeSelection = false)
    {
        if (clearTypeSelection) TypeSelection = EditorAssetSelection.None;
        AssetObjects.Clear();
        AssetFileObjects.Clear();
    }
}

public sealed class EntityListViewModel
{
    public int SelectedEntityId { get; set; } = 0;
    public List<EntityViewModel> Entities { get; } = new(128);

    public void ResetState()
    {
        SelectedEntityId = 0;
        Entities.Clear();
    }
}

public record AssetObjectViewModel(
    int AssetId,
    int ResourceId,
    string ResourceName,
    string Name,
    bool IsCoreAsset,
    int Generation,
    string SpecialName,
    string SpecialValue,
    bool HasActions);

public sealed record AssetObjectFileViewModel(
    int AssetFileId,
    string RelativePath,
    long SizeInBytes,
    string? ContentHash);

public sealed class EntityViewModel(
    int entityId,
    string name,
    int componentCount,
    in EntityEditorModel model,
    in EntityEditorTransform transform
)
{
    public int EntityId { get; } = entityId;
    public string Name { get; } = name;
    public int ComponentCount { get; } = componentCount;
    public EntityEditorModel Model { get; set; } = model;

    private EntityEditorTransform _transform = transform;
    public ref EntityEditorTransform Transform => ref _transform;
}

public struct EntityEditorTransform(in Vector3 translation, in Vector3 scale, in Quaternion rotation)
{
    public Vector3 Translation = translation;
    public Vector3 Scale = scale;
    public Vector3 EulerAngles = Vector3.Zero;
    public Quaternion Rotation = rotation;
}

public readonly record struct EntityEditorModel(int ModelId, int MaterialTagKey, int DrawCount);