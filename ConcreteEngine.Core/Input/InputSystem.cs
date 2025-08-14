#region

using Silk.NET.Input;
using Silk.NET.Maths;

#endregion

namespace ConcreteEngine.Core.Input;

public sealed class InputSystem: IGameEngineSystem
{
    private readonly KeyboardInput _keyboardInput;
    private readonly MouseInput _mouseInput;

    public InputSystem(IInputContext input)
    {
        var keyboard = input.Keyboards.First();
        var mouse = input.Mice.First();

        _keyboardInput = new KeyboardInput(keyboard);
        _mouseInput = new MouseInput(mouse);
    }

    public void Update()
    {
        _keyboardInput.Update();
        _mouseInput.Update();
    }

    // Keyboard API
    public bool IsKeyDown(Key key) => _keyboardInput.IsKeyDown(key);
    public bool IsKeyPressed(Key key) => _keyboardInput.IsKeyPressed(key);
    public bool IsKeyReleased(Key key) => _keyboardInput.IsKeyReleased(key);

    // Mouse API
    public bool IsMouseDown(MouseButton button) => _mouseInput.IsMouseDown(button);
    public bool IsMousePressed(MouseButton button) => _mouseInput.IsMousePressed(button);
    public bool IsMouseReleased(MouseButton button) => _mouseInput.IsMouseReleased(button);

    public Vector2D<float> MousePosition => _mouseInput.MousePosition;
    public Vector2D<float> MouseDelta => _mouseInput.MouseDelta;
    
    public void Dispose()
    {
    }
}