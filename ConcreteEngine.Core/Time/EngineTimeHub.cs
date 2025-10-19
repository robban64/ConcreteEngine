#region

using ConcreteEngine.Core.Data;

#endregion

namespace ConcreteEngine.Core.Time;

internal sealed class EngineTimeHub
{
    private readonly GameTickScheduler _gameTickScheduler;
    private readonly RenderTickScheduler _renderTickScheduler;
    public DebounceTicker? DebounceTicker { get; set; } = null;

    public float Alpha => _gameTickScheduler.Alpha;

    //TODO
    public EngineTimeHub(UpdateTickDelegate onGameTick, UpdateTickDelegate onUpdateLogTick,
        RenderTickDelegate onGfxUpload, RenderTickDelegate onGfxDispose)
    {
        _gameTickScheduler = new GameTickScheduler(onGameTick, onUpdateLogTick);
        _renderTickScheduler = new RenderTickScheduler(onGfxUpload, onGfxDispose);
    }

    public void UpdateFrame(float dt)
    {
        _gameTickScheduler.Advance(dt);
    }

    public void RenderFrame(float dt)
    {
        _renderTickScheduler.Accumulate(dt);
        _renderTickScheduler.Advance();
    }

    private void OnGameTickUpdate(int tick)
    {
    }

    private void OnFpsTickUpdate(int tick)
    {
    }


    private void OnGfxTickDispose(int tick)
    {
    }

    private void OnGfxTickUpload(int tick)
    {
    }
}