using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.ViewModel;

internal sealed class CameraViewModel
{
    public long Generation { get; set; } = 0;

    private CameraEditorPayload _data;
    private CameraDataState _state;
    
    public ref readonly CameraEditorPayload Data => ref _data;
    public ref CameraDataState DataState => ref _state;
    
    public void InitData(in CameraEditorPayload data)
    {
        _data = data;
        _state.Transform.From(in data.ViewTransform);
        _state.Projection = CameraProjectionState.From(in data.Projection);
    }

    public void UpdateData(in CameraEditorPayload data)
    {
        _data = data;
        _state.Transform.FromStable(in data.ViewTransform);
        _state.Projection = CameraProjectionState.From(in data.Projection);
    }
}