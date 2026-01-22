using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine.Input;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Platform;

internal sealed class EngineInputSource : IDisposable
{
    public const int ButtonCapacity = 16;
    private const float SmoothFactor = 0.2f;
    private const float Epsilon = 0.001f;

    private readonly IInputContext _context;
    private readonly IKeyboard _keyboard;
    private readonly IMouse _mouse;

    private readonly Dictionary<Key, InputButtonState> _keyState = new(16);

    private readonly List<Key> _activeKeys = new(16);
    private readonly List<Key> _keysToRemove = new(16);

    private readonly List<char> _keyChars = new(32);

    private readonly InputButtonState[] _mouseButtonState = new InputButtonState[ButtonCapacity];

    private Vector2 _mousePosition;
    private Vector2 _lastMousePosition;

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
        _keyboard.KeyChar += OnKeyChar;

        _mouse.MouseDown += OnMouseDown;
        _mouse.MouseUp += OnMouseUp;
        _mouse.MouseMove += OnMouseMove;
        _mouse.Scroll += OnMouseScroll;
    }

    public bool HasEmptyKeyChars => _keyChars.Count == 0;
    public bool HasEmptyKeyInput => _activeKeys.Count == 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<InputButtonState> MouseButtons() => _mouseButtonState.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<Key> GetActiveKeys() => CollectionsMarshal.AsSpan(_activeKeys);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> GetKeyChars() => CollectionsMarshal.AsSpan(_keyChars);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasKey(Key key, out InputButtonState state) => _keyState.TryGetValue(key, out state);


    public void Clear()
    {
        _mousePosition = default;
        _accumScroll = default;
        _lastMouseScroll = default;

        _activeMouseButtonCount = 0;

        _keysToRemove.Clear();
        _keyState.Clear();
        Array.Clear(_mouseButtonState);
    }

    public void ClearKeyChar()
    {
        _keyChars.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ClearFrameInput()
    {
        // Keys
        foreach (var key in _keysToRemove)
            _keyState.Remove(key);

        _activeKeys.Clear();
        _keysToRemove.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdateMousePosition(out InputMouseState mouseState)
    {
        var step = (_accumScroll - _lastMouseScroll) * SmoothFactor;
        var scroll = step;
        if (float.Abs(step.X) > Epsilon || float.Abs(step.Y) > Epsilon)
        {
            _lastMouseScroll += step;
        }
        else
        {
            scroll = Vector2.Zero;
            _lastMouseScroll = _accumScroll;
        }

        var delta = _mousePosition - _lastMousePosition;
        _lastMousePosition = _mousePosition;

        mouseState = new InputMouseState { Position = _mousePosition, Delta = delta, Scroll = scroll };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update()
    {
        foreach (var key in _keyState.Keys)
        {
            ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(_keyState, key, out _);
            state.Update();
            if (state is { Up: true, Pressed: false })
                _keysToRemove.Add(key);

            _activeKeys.Add(key);
        }

        // Mouse
        var span = new UnsafeSpan<InputButtonState>(_mouseButtonState.AsSpan(0, _activeMouseButtonCount));
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

    private void OnKeyChar(IKeyboard keyboard, char key)
    {
        _keyChars.Add(key);
    }
/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref InputButtonState FindKeyState(List<(Key, InputButtonState)> keyStates, Key key, out int index)
    {
        var span = CollectionsMarshal.AsSpan(keyStates);
        for (var i = 0; i < keyStates.Count; i++)
        {
            ref var state = ref span[i];
            if (keyStates[i].Item1 == key)
            {
                index = i;
                return ref state.Item2;
            }
        }

        index = -1;
        return ref Unsafe.NullRef<InputButtonState>();
    }*/

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