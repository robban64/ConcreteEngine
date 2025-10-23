#region

using ConcreteEngine.Core.Data;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.RenderingSystem;
using ConcreteEngine.Core.Scene.Modules;

#endregion

namespace Demo3D;

internal sealed class EffectAdjustModule : GameModule
{
    private IEngineInputSource _input;
    private RenderSceneProps _renderProps;
    private float _value = 0.0f;
    private float _fvalue = 0.0f;

    public override void Initialize()
    {
        _input = Context.GetSystem<IInputSystem>().InputSource;
        var renderer = Context.GetSystem<IRenderingSystem>();
        _renderProps = renderer.SceneProperties;
    }

    public override void Update(in UpdateTickInfo frameCtx)
    {
    }
}