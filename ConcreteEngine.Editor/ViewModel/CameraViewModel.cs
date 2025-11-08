using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.ViewModel;

public sealed class CameraViewModel
{
    public long Generation { get; set; } = 0;

    private CameraEditorPayload _model;

    public ref readonly CameraEditorPayload Model => ref _model;
    public ref readonly ViewTransformData Transform => ref _model.ViewTransform;
    public ref readonly ProjectionInfoData Projection => ref _model.Projection;
    public ref readonly Size2D Viewport => ref _model.Viewport;


    public void FromDataModel(in CameraEditorPayload model)
    {
        _model = model;
    }
}