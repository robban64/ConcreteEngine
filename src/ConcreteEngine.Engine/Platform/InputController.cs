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

    public bool Enabled { get; internal set; } = true;

    public InputLayerKind Kind { get; }

    internal InputLayer(EngineInputSource source, InputLayerKind kind)
    {
        _source = source;
        Kind = kind;
    }


    // Keyboard API
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyDown(Key key) => Enabled && _source.HasKey(key, out var state) && state.IsHeld;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyPressed(Key key) => Enabled && _source.HasKey(key, out var state) && state.Pressed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyUp(Key key) => Enabled && _source.HasKey(key, out var state) && state.Up;

    // Mouse API
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMouseDown(MouseButton button) => Enabled && _source.MouseButtons()[(int)button].IsHeld;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMousePressed(MouseButton button) => Enabled && _source.MouseButtons()[(int)button].Pressed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMouseUp(MouseButton button) => Enabled && _source.MouseButtons()[(int)button].Up;
}