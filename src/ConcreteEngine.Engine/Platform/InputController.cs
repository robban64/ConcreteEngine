using System.Runtime.CompilerServices;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Platform;

public enum InputLayerKind
{
    Ui,
    Game
}

public sealed class InputLayer
{
    private readonly EngineInputSource _source;

    public bool MouseEnabled { get; internal set; } = true;
    public bool KeyboardEnabled { get; internal set; } = true;

    public InputLayerKind LayerKind { get; }

    internal InputLayer(EngineInputSource source, InputLayerKind layerKind)
    {
        _source = source;
        LayerKind = layerKind;
    }

    // Keyboard API
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyDown(Key key) => KeyboardEnabled && _source.HasKey(key, out var state) && state.IsHeld;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyPressed(Key key) => KeyboardEnabled && _source.HasKey(key, out var state) && state.Pressed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyUp(Key key) => KeyboardEnabled && _source.HasKey(key, out var state) && state.Up;

    // Mouse API
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMouseDown(MouseButton button) => MouseEnabled && _source.MouseButtons()[(int)button].IsHeld;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMousePressed(MouseButton button) => MouseEnabled && _source.MouseButtons()[(int)button].Pressed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMouseUp(MouseButton button) => MouseEnabled && _source.MouseButtons()[(int)button].Up;
}