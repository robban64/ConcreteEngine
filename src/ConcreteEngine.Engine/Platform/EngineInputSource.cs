using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Engine.Metadata.Input;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Platform;

internal sealed class EngineInputSource : IDisposable
{
    internal const int ButtonCapacity = 16;
    private const float SmoothFactor = 0.2f;
    private const float Epsilon = 0.001f;

    private readonly IInputContext _context;
    private readonly IKeyboard _keyboard;
    private readonly IMouse _mouse;


    private readonly Dictionary<Key, InputButtonState> _keyState = new(8);
    private readonly List<Key> _keysToRemove = new(8);

    private readonly InputButtonState[] _mouseButtonState = new InputButtonState[ButtonCapacity];
    
    private InputMouseState _mouseState;

    private Vector2 _mousePosition;
    private Vector2 _accumScroll;
    private Vector2 _lastMouseScroll;

    private int _activeMouseButtonCount;
    
    public EngineInputSource(IInputContext input)
    {
        _context = input;
        _keyboard = input.Keyboards[0];
        _mouse = input.Mice[0];


        _keyboard.KeyDown += OnKeyDown;
        _keyboard.KeyUp += OnKeyUp;

        _mouse.MouseDown += OnMouseDown;
        _mouse.MouseUp += OnMouseUp;
        _mouse.MouseMove += OnMouseMove;
        _mouse.Scroll += OnMouseScroll;
    }
    
    internal Dictionary<Key, InputButtonState> GetKeyState() => _keyState;
    
    public ref readonly InputMouseState MouseState => ref _mouseState;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<InputButtonState> MouseButtons() => _mouseButtonState.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool HasKey(Key key, out InputButtonState state) => _keyState.TryGetValue(key, out state);

    
    internal void Clear()
    {
        _mousePosition = default;
        _mouseState = default;

        _accumScroll = default;
        _lastMouseScroll = default;
        
        _activeMouseButtonCount = 0;

        _keysToRemove.Clear();
        _keyState.Clear();
        Array.Clear(_mouseButtonState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Update()
    {
        UpdateMousePosition();
        
        // Keys
        foreach (var key in _keysToRemove)
            _keyState.Remove(key);

        _keysToRemove.Clear();

        foreach (var key in _keyState.Keys)
        {
            ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_keyState, key, out _);
            state.Update();
            if (state is { Up: true, Pressed: false })
                _keysToRemove.Add(key);
        }
        
        // Mouse
        var span = new UnsafeSpan<InputButtonState>(_mouseButtonState, _activeMouseButtonCount);
        foreach (var state in span)
        {
            if (state.Value is { Down: false, WasDown: false, Up: false }) continue;
            state.Value.Update();

            if (state.Value is { Up: true, WasDown: false })
            {
                state.Value = default;
                _activeMouseButtonCount--;
            }
        }

        if (_activeMouseButtonCount < 0) _activeMouseButtonCount = 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UpdateMousePosition()
    {
        var step = (_accumScroll - _lastMouseScroll) * SmoothFactor;
        if (float.Abs(step.X) > Epsilon || float.Abs(step.Y) > Epsilon)
        {
            _mouseState.Scroll = step;
            _lastMouseScroll += step;
        }
        else
        {
            _mouseState.Scroll = Vector2.Zero;
            _lastMouseScroll = _accumScroll;
        }

        var delta = _mousePosition - _mouseState.Position;
        _mouseState = _mouseState with { Position = _mousePosition, Delta = delta };
    }


    // Keyboard API
    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_keyState, key, out _);
        state.Down = true;
        state.Up = false;
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_keyState, key, out bool exists);
        if (exists) state.Up = true;
    }

    // Mouse API
    private void OnMouseMove(IMouse _, Vector2 position) => _mousePosition = position;
    private void OnMouseScroll(IMouse _, ScrollWheel scroll) => _accumScroll += new Vector2(scroll.X, scroll.Y);

    private void OnMouseDown(IMouse _, MouseButton button)
    {
        int index = (int)button;
        if ((uint)index >= _mouseButtonState.Length) return;

        ref var buttonState = ref _mouseButtonState[index];
        if (!buttonState.Down)
            _activeMouseButtonCount++;

        buttonState.Down = true;
        buttonState.Up = false;
    }

    private void OnMouseUp(IMouse _, MouseButton button)
    {
        int index = (int)button;
        if ((uint)index >= _mouseButtonState.Length) return;
        _mouseButtonState[index].Up = true;
    }


    public void Dispose()
    {
        _keyboard.KeyDown -= OnKeyDown;
        _keyboard.KeyUp -= OnKeyUp;
        _mouse.MouseDown -= OnMouseDown;
        _mouse.MouseUp -= OnMouseUp;
        _mouse.MouseMove -= OnMouseMove;
        _mouse.Scroll -= OnMouseScroll;
    }
}