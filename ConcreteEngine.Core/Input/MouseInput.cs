#region

using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Input;

internal class MouseInput
{
    private readonly IMouse _mouse;
    private readonly HashSet<MouseButton> _buttonsDown = [];
    private readonly HashSet<MouseButton> _buttonsPressed = [];
    private readonly HashSet<MouseButton> _buttonsReleased = [];

    public Vector2D<float> MousePosition { get; private set; }
    public Vector2D<float> MouseDelta { get; private set; }

    private Vector2D<float> _lastMousePosition;

    public MouseInput(IMouse mouse)
    {
        _mouse = mouse;
        _mouse.MouseDown += OnMouseDown;
        _mouse.MouseUp += OnMouseUp;
        _mouse.MouseMove += OnMouseMove;

        MousePosition = _mouse.Position.ToGeneric();
        _lastMousePosition = MousePosition;
    }

    public void Update()
    {
        _buttonsPressed.Clear();
        _buttonsReleased.Clear();

        MouseDelta = MousePosition - _lastMousePosition;
        _lastMousePosition = MousePosition;
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        if (_buttonsDown.Add(button))
        {
            _buttonsPressed.Add(button);
        }
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        _buttonsDown.Remove(button);
        _buttonsReleased.Add(button);
    }

    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        MousePosition = position.ToGeneric();
    }

    public bool IsMouseDown(MouseButton button) => _buttonsDown.Contains(button);
    public bool IsMousePressed(MouseButton button) => _buttonsPressed.Contains(button);
    public bool IsMouseReleased(MouseButton button) => _buttonsReleased.Contains(button);
}