using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.ViewModel;

internal sealed class CameraViewModel
{
    private CameraEditorPayload _data;
    private CameraDataState _state;

    public ref CameraEditorPayload Data => ref _data;
    public ref CameraDataState DataState => ref _state;

    public unsafe void UpdateState(in ApiWriteRequest<CameraEditorPayload> api)
    {
        api.DispatchWrite(ref _data);
        if(_state.Generation == Data.Generation) return;

        _state.Transform.Translation = _data.ViewTransform.Translation;
        _state.Transform.Scale = _data.ViewTransform.Scale;
        _state.Transform.Orientation = _data.ViewTransform.Orientation.AsVec2();
        _state.Projection = CameraProjectionState.From(in _data.Projection);
        _state.Generation = _data.Generation;
    }
}