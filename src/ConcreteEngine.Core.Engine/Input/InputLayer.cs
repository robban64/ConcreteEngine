using System.Runtime.CompilerServices;
using Silk.NET.Input;
using static ConcreteEngine.Core.Engine.Input.EngineInput;

namespace ConcreteEngine.Core.Engine.Input;

public sealed class InputLayer
{
    public bool Enabled { get; internal set; }
    public InputLayerKind Kind { get; }

    internal InputLayer(InputLayerKind kind)
    {
        Kind = kind;
    }

    // Keyboard API
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyDown(Key key) => Enabled && Keyboard.HasKey(key, out var state) && state.IsHeld;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyPressed(Key key) => Enabled && Keyboard.HasKey(key, out var state) && state.Pressed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyUp(Key key) => Enabled && Keyboard.HasKey(key, out var state) && state.Up;

    // Mouse API
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMouseDown(MouseButton button) => Enabled && Mouse.GetButton(button).IsHeld;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMousePressed(MouseButton button) => Enabled && Mouse.GetButton(button).Pressed;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMouseUp(MouseButton button) => Enabled && Mouse.GetButton(button).Up;
}