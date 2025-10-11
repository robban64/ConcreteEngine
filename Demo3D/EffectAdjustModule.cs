using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Data;
using ConcreteEngine.Core.Engine.Platform;
using ConcreteEngine.Core.Modules;
using ConcreteEngine.Core.Rendering;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Core.Rendering.State;
using Silk.NET.Input;

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
        var renderer = Context.GetSystem<IRenderSystem>();
        _renderProps = renderer.RenderProps;
    }

    public override void Update(in UpdateTickInfo frameCtx)
    {

    }
}