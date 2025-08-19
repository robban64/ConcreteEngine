#region

using System.Numerics;
using Silk.NET.Input;

#endregion

namespace ConcreteEngine.Core.Platform;

internal class MouseInput: IDisposable
{
    private const int BufferSize = 16;
    private readonly IMouse _mouse;
    private readonly byte[] _buttonsDown = new byte[BufferSize];
    private readonly byte[] _buttonsPressed = new byte[BufferSize];
    private readonly byte[] _buttonsReleased = new byte[BufferSize];

    public Vector2 MousePosition { get; private set; }
    public Vector2 MouseDelta { get; private set; }

    private Vector2 _lastMousePosition;

    public MouseInput(IMouse mouse)
    {
        _mouse = mouse;
        _mouse.MouseDown += OnMouseDown;
        _mouse.MouseUp += OnMouseUp;
        _mouse.MouseMove += OnMouseMove;

        MousePosition = _mouse.Position;
        _lastMousePosition = MousePosition;
    }

    public void Update()
    {
        for (var i = 0; i < BufferSize; i++)
        {
            _buttonsPressed[i] = 0;
            _buttonsReleased[i] = 0;
        }

        MouseDelta = MousePosition - _lastMousePosition;
        _lastMousePosition = MousePosition;
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if((int)button < 0) return;
        
        if (_buttonsDown[(int)button] == 0)
        {
            _buttonsPressed[(int)button] = 1;
        }

        _buttonsDown[(int)button] = 1;
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        if((int)button < 0) return;

        _buttonsDown[(int)button] = 0;
        _buttonsReleased[(int)button] = 1;
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        MousePosition = position;
    }

    public bool IsMouseDown(MouseButton button) => _buttonsDown[(int)button] == 1;
    public bool IsMousePressed(MouseButton button) => _buttonsPressed[(int)button] == 1;
    public bool IsMouseReleased(MouseButton button) => _buttonsReleased[(int)button] == 1;
    
    public void Dispose()
    {
        _mouse.MouseDown -= OnMouseDown;
        _mouse.MouseUp -= OnMouseUp;
        _mouse.MouseMove -= OnMouseMove;
    }
}