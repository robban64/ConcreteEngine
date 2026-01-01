using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Diagnostics.Time;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Platform;

internal sealed class EngineInputSource : IDisposable
{
    private const int ButtonCapacity = 16;
    private const float SmoothFactor = 0.2f;
    private const float Epsilon = 0.001f;

    private readonly IInputContext _context;
    private readonly IKeyboard _keyboard;
    private readonly IMouse _mouse;

    internal readonly Dictionary<Key, ButtonState> KeyState = new(8);
    internal readonly ButtonState[] ButtonState = new ButtonState[ButtonCapacity];
    internal MouseStateSnapshot MouseState;

    private readonly List<Key> _keysToRemove = new(8);
    private MouseStateSnapshot _lastMouse;

    private Vector2 _mousePosition;
    private Vector2 _accumScroll;

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

    internal void Clear()
    {
        _mousePosition = default;
        _accumScroll = default;
        _activeMouseButtonCount = 0;
        MouseState = default;
        _lastMouse = default;
        
        _keysToRemove.Clear();
        KeyState.Clear();
        Array.Clear(ButtonState);
        
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Update(float dt)
    {
        UpdateMousePosition(dt);
        UpdateMouseButtons();
        UpdateKeyInput();
    }

    private void UpdateMousePosition(float dt)
    {
        MouseState.MousePosition = _mousePosition;
        MouseState.MouseDelta = _mousePosition - _lastMouse.MousePosition;
        _accumScroll *= dt;
        var step = (_accumScroll - _lastMouse.Scroll) * SmoothFactor;

        var abs = Vector2.Abs(step);
        if (abs.X > Epsilon || abs.Y > Epsilon)
            MouseState.Scroll += step;
        else
            _accumScroll = MouseState.Scroll = Vector2.Zero;

        _lastMouse = MouseState;
    }

    private void UpdateKeyInput()
    {
        foreach (var key in _keysToRemove)
            KeyState.Remove(key);

        _keysToRemove.Clear();

        foreach (var key in KeyState.Keys)
        {
            ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(KeyState, key, out _);
            state.Update();
            if (state is { Up: true, Pressed: false } || state == default)
                _keysToRemove.Add(key);
        }
    }

    private void UpdateMouseButtons()
    {
        if (_activeMouseButtonCount <= 0) return;

        for (int i = 0; i < ButtonState.Length; i++)
        {
            ref var state = ref ButtonState[i];

            if (state is { Down: false, WasDown: false, Up: false }) continue;
            state.Update();

            if (state is { Up: true, WasDown: false })
            {
                state = default;
                _activeMouseButtonCount--;
            }
        }

        if (_activeMouseButtonCount < 0) _activeMouseButtonCount = 0;
    }


    // Keyboard API
    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(KeyState, key, out _);
        state.Down = true;
        state.Up = false;
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        ref var state = ref CollectionsMarshal.GetValueRefOrAddDefault(KeyState, key, out bool exists);
        if (exists) state.Up = true;
    }

    // Mouse API
    private void OnMouseMove(IMouse _, Vector2 position) => _mousePosition = position;
    private void OnMouseScroll(IMouse _, ScrollWheel scroll) => _accumScroll += new Vector2(scroll.X, scroll.Y);

    private void OnMouseDown(IMouse _, MouseButton button)
    {
        int index = (int)button;
        if ((uint)index >= ButtonState.Length) return;

        ref var buttonState = ref ButtonState[index];
        if (!buttonState.Down)
            _activeMouseButtonCount++;

        buttonState.Down = true;
        buttonState.Up = false;
    }

    private void OnMouseUp(IMouse _, MouseButton button)
    {
        int index = (int)button;
        if ((uint)index >= ButtonState.Length) return;
        ButtonState[index].Up = true;
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