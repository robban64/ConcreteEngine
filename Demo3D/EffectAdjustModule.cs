#region

using ConcreteEngine.Engine.Data;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Worlds.Render;

#endregion

namespace Demo3D;

internal sealed class EffectAdjustModule : GameModule
{
    private IEngineInputSource _input;
    private WorldRenderParams _worldRenderParams;
    private float _value = 0.0f;
    private float _fvalue = 0.0f;

    public override void Initialize()
    {
        _input = Context.GetSystem<IInputSystem>().InputSource;
        var renderer = Context.GetSystem<IWorldRenderer>();
        _worldRenderParams = renderer.WorldRenderParams;
    }

    public override void Update(in UpdateTickInfo frameCtx)
    {
    }
}