using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Input;

namespace ConcreteEngine.Engine.Platform;

public sealed class InputSystem : GameEngineSystem
{
    private InputMouseState _mouseState;
    public Vector2 MouseUv {get; private set;}

    private readonly List<InputLayer> _layers;
    internal EngineInputSource Source { get; }

    internal InputSystem(EngineInputSource source)
    {
        Source = source;
        _layers =
        [
            new InputLayer(source, InputLayerKind.Ui) { Enabled = false },
            new InputLayer(source, InputLayerKind.Game) { Enabled = true },
        ];
    }

    public ref InputMouseState MouseState => ref _mouseState;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public InputLayer GetLayer(InputLayerKind kind) => _layers[(int)kind];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ActiveAllLayers()
    {
        foreach (var layer in _layers)
            layer.Enabled = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetActiveLayer(InputLayerKind kind)
    {
        foreach (var layer in _layers)
            layer.Enabled = kind == layer.Kind;
    }

    internal void Update(Size2D outputSize)
    {
        ref var mouseState = ref _mouseState;
        Source.ClearFrameInput();
        Source.UpdateMousePosition(out mouseState);
        Source.Update();

        MouseUv = CoordinateMath.ToUvCoords(mouseState.Position,outputSize);

    }

    internal void EndFrame() => Source.ClearKeyChar();

    internal void ClearInputState() => Source.Clear();
}