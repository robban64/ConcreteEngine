using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Shared.TransformData;

namespace ConcreteEngine.Editor.ViewModel;

internal sealed class CameraViewModel
{
    public long Generation { get; private set; }

    private CameraEditorPayload _data;
    private CameraDataState _state;

    public ref CameraEditorPayload Data => ref _data;
    public ref CameraDataState DataState => ref _state;

    
    public void WriteTo(in ApiWriteRequest<CameraEditorPayload> api)
    {
        var engineGen = api.WriteTo(Generation, ref _data);
        if(Generation == engineGen) return;
        
        _state.Transform.From(in _data.ViewTransform);
        _state.Projection = CameraProjectionState.From(in _data.Projection);
        Generation =  engineGen;
    }

    public void WriteFrom(in ApiWriteRequest<CameraEditorPayload> api)
    {
        Generation++;
        _state.Fill(Generation, _data.Viewport, out _data);
        api.WriteFrom(_data.Generation, ref _data);
    }
}