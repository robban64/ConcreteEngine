using ConcreteEngine.Common.Numerics;

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
    private TransformEditorModel _transform;
    private ProjectionEditorModel _projection;
    private Size2D _viewport;

    public ref TransformEditorModel Transform => ref _transform;
    public ref ProjectionEditorModel Projection => ref _projection;
    public ref Size2D Viewport => ref _viewport;


    public void FromDataModel(in CameraEditorModel model)
    {
        var prevEuler = Transform.EulerAngles;
        _transform = model.Transform;
        _projection = model.Projection;
        _viewport = model.Viewport;

        _transform.EulerAngles = prevEuler;
    }

    public void ToDataModel(out CameraEditorModel model) =>
        model = new CameraEditorModel(in _transform, in _projection, in _viewport);
}