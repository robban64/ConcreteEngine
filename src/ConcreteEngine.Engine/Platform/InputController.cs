using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Platform;

public sealed class InputController
{
    private EngineInputSource _source;

    internal InputController(EngineInputSource source)
    {
        _source = source;
    }

    // Keyboard API
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyDown(Key key) => _source.KeyState.TryGetValue(key, out var state) && state.IsDown;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyPressed(Key key) => _source.KeyState.TryGetValue(key, out var state) && state.Pressed;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsKeyUp(Key key) => _source.KeyState.TryGetValue(key, out var state) && state.Up;

    // Mouse API
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMouseDown(MouseButton button) => _source.ButtonState[(int)button].IsDown;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMousePressed(MouseButton button) => _source.ButtonState[(int)button].Pressed;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsMouseUp(MouseButton button) => _source.ButtonState[(int)button].Up;
    
    public MouseStateSnapshot MouseState => _source.MouseState;


}