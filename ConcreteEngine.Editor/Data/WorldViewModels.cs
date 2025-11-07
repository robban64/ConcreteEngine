#region

using ConcreteEngine.Common.Numerics;

#endregion

namespace ConcreteEngine.Editor.Data;

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

public sealed class EntityViewModel(
    int entityId,
    string name,
    int componentCount,
    in EntityEditorModel model,
    in TransformEditorModel transform
)
{
    public int EntityId { get; } = entityId;
    public string Name { get; } = name;
    public int ComponentCount { get; } = componentCount;
    public EntityEditorModel Model { get; set; } = model;

    private TransformEditorModel _transform = transform;
    public ref TransformEditorModel Transform => ref _transform;
}

public sealed class CameraViewModel
{
    public long Generation { get; set; } = 0;

    private CameraEditorPayload _model;

    public ref CameraEditorPayload Model => ref _model;

    public ref ViewTransformEditorModel Transform => ref _model.ViewTransform;
    public ref ProjectionEditorModel Projection => ref _model.Projection;
    public ref Size2D Viewport => ref _model.Viewport;


    public void FromDataModel(in CameraEditorPayload model)
    {
        _model = model;
    }
}