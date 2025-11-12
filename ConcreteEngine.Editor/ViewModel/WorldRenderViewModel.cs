using System.Numerics;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.DataState;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Shared.RenderData;

namespace ConcreteEngine.Editor.ViewModel;

internal sealed class WorldRenderViewModel
{
    private WorldParamState _dataState;

    public WorldParamSelection Selection { get; set; }

    public ref WorldParamState DataState => ref _dataState;

    public ref LightState LightState => ref _dataState.LightState;
    public ref FogState FogState => ref _dataState.FogState;
    public ref PostEffectState PostState => ref _dataState.PostState;

    public void UpdateState(in ApiWriteRequest<WorldParamState> api) =>
        api.DispatchWrite(ref _dataState);
}