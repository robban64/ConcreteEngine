#region

using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Definitions;

#endregion

namespace ConcreteEngine.Editor.ViewModel;

internal sealed class WorldRenderViewModel
{
    public long Version { get; private set; }

    private WorldParamState _dataState;

    public WorldParamSelection Selection { get; set; }

    public ref WorldParamState DataState => ref _dataState;

    public ref LightState LightState => ref _dataState.LightState;
    public ref FogState FogState => ref _dataState.FogState;
    public ref PostEffectState PostState => ref _dataState.PostState;

    public void FillData(in ApiDataRefRequest<WorldParamState> api) => api.FillData(Version, ref _dataState);

    public void WriteData(in ApiDataRefRequest<WorldParamState> api)
    {
        Version++;
        api.WriteData(Version, ref _dataState);
    }
}