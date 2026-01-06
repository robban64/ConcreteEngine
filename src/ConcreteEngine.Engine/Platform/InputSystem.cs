using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Engine;

namespace ConcreteEngine.Engine.Platform;

public sealed class InputSystem : GameEngineSystem
{

    private InputMouseState _mouseState;

    private readonly List<InputLayer> _layers;
    private readonly EngineInputSource _source;


    internal EngineInputSource Source => _source;

    internal InputSystem(EngineInputSource source)
    {
        _source = source;
        _layers =
        [
            new InputLayer(source, InputLayerKind.Ui)  { Enabled = false},
            new InputLayer(source, InputLayerKind.Game) { Enabled = true},
        ];
    }

    public ref InputMouseState MouseState => ref _mouseState;

    public InputLayer GetLayer(InputLayerKind kind) => _layers[(int)kind];

    public void ActiveAllLayers()
    {
        foreach (var layer in _layers)
            layer.Enabled =(true);
    }

    public void SetActiveLayer(InputLayerKind kind)
    {
        foreach (var layer in _layers)
            layer.Enabled = kind == layer.Kind;
    }


    internal void Update()
    {
        ref var mouseState = ref _mouseState;
        _source.ClearFrameInput();
        _source.UpdateMousePosition(out mouseState);
        _source.Update();
    }

    internal void EndFrame() => _source.ClearKeyChar();

    internal void ClearInputState() => _source.Clear();
}