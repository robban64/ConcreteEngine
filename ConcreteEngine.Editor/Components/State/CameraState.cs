#region

using System.Diagnostics;
using ConcreteEngine.Editor.Components.Data;
using ConcreteEngine.Editor.Data;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class CameraState
{
    private long _generation;
    private CameraDataState _data;
    private CameraDataState _state;

    public ref readonly CameraDataState Data => ref _data;
    public ref CameraDataState DataState => ref _state;
    public long Generation => _generation;

    public void Dispatch(ApiRefRequest<CameraDataState> api, bool isWriteRequest)
    {
        if (isWriteRequest)
        {
            var request = new EditorDataRequest<CameraDataState>(ref _generation, ref _state, isWriteRequest);
            api(ref request);
            if (request.HasNewData) _data = _state;
        }
        else
        {
            var request = new EditorDataRequest<CameraDataState>(ref _generation, ref _data, isWriteRequest);
            api(ref request);
            if (request.HasNewData) _state = _data;
        }
    }

}