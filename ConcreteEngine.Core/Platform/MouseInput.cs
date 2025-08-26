#region

using System.Numerics;
using Silk.NET.Input;

#endregion

namespace ConcreteEngine.Core.Platform;

internal class MouseInput : IDisposable
{
    private const int BufferSize = 16;
    private readonly IMouse _mouse;
    private readonly byte[] _buttonsDown = new byte[BufferSize];
    private readonly byte[] _buttonsPressed = new byte[BufferSize];
    private readonly byte[] _buttonsReleased = new byte[BufferSize];

    public Vector2 Position { get; private set; }
    public Vector2 PositionDelta { get; private set; }
    public Vector2 Scroll { get; private set; }

    private Vector2 _lastPos;

    public MouseInput(IMouse mouse)
    {
        _mouse = mouse;
        _mouse.MouseDown += OnMouseDown;
        _mouse.MouseUp += OnMouseUp;
        _mouse.MouseMove += OnMouseMove;

        Position = _mouse.Position;
        _lastPos = Position;
    }

    public void Update()
    {
        for (var i = 0; i < BufferSize; i++)
        {
            _buttonsPressed[i] = 0;
            _buttonsReleased[i] = 0;
        }

        PositionDelta = Position - _lastPos;
        _lastPos = Position;

        var scrollWheel = _mouse.ScrollWheels[0];
        Scroll = new Vector2(scrollWheel.X, scrollWheel.Y);
        
    }


    private void OnScroll(IMouse mouse, ScrollWheel scroll)
    {
        Scroll = new Vector2(scroll.X, scroll.Y);
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        Position = position;
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if ((int)button < 0) return;

        if (_buttonsDown[(int)button] == 0)
        {
            _buttonsPressed[(int)button] = 1;
        }

        _buttonsDown[(int)button] = 1;
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        if ((int)button < 0) return;

        _buttonsDown[(int)button] = 0;
        _buttonsReleased[(int)button] = 1;
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