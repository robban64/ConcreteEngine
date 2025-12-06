#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Shared.RenderData;

#endregion

namespace ConcreteEngine.Editor.Components.State;

internal sealed class WorldParamState
{
    private long _generation;

    private WorldParamsData _dataState;
    public ref WorldParamsData DataState => ref _dataState;

    public WorldParamSelection Selection { get; set; }

    public long Generation => _generation;

    public void Dispatch(ApiRefRequest<WorldParamsData> api, bool isWriteRequest)
    {
        var request = new EditorDataRequest<WorldParamsData>(ref _generation, ref _dataState, isWriteRequest);
        api(ref request);
    }
}