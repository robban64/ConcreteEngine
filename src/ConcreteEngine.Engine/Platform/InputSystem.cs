using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Input;

namespace ConcreteEngine.Engine.Platform;

public sealed class InputSystem : IGameEngineSystem
{
    private readonly List<InputLayer> _layers;

    public MouseState MouseState { get; }
    internal EngineInputSource Source { get; }

    internal InputSystem(EngineInputSource source)
    {
        Source = source;
        MouseState = new MouseState();
        _layers =
        [
            new InputLayer(source, InputLayerKind.Ui) { Enabled = false },
            new InputLayer(source, InputLayerKind.Game) { Enabled = true }
        ];
    }


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

    internal void Update(Vector2I viewportPos)
    {
        Source.ClearFrameInput();
        Source.Update();

        Source.UpdateMousePosition(out var pos, out var scroll, out var delta);
        MouseState.Set(pos, scroll, delta, viewportPos);
    }

    internal void EndFrame() => Source.ClearKeyChar();

    internal void ClearInputState() => Source.Clear();
    
    public void Shutdown() {}

}