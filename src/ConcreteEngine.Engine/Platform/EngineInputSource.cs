using System.Numerics;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Platform;

public sealed class EngineInputSource
{
    private readonly IInputContext _context;
    private readonly KeyboardInput _keyboardInput;
    private readonly MouseInput _mouseInput;

    internal IInputContext InputContext => _context;

    public EngineInputSource(IInputContext input)
    {
        _context = input;
        var keyboard = input.Keyboards[0];
        var mouse = input.Mice[0];

        _keyboardInput = new KeyboardInput(keyboard);
        _mouseInput = new MouseInput(mouse);
    }


    internal void Update(bool enableInput)
    {
        _keyboardInput.Update(enableInput);
        _mouseInput.Update(enableInput);
    }

    // Keyboard API
    public bool IsKeyDown(Key key) => _keyboardInput.IsKeyDown(key);
    public bool IsKeyPressed(Key key) => _keyboardInput.IsKeyPressed(key);
    public bool IsKeyReleased(Key key) => _keyboardInput.IsKeyReleased(key);

    // Mouse API
    public bool IsMouseDown(MouseButton button) => _mouseInput.IsMouseDown(button);
    public bool IsMousePressed(MouseButton button) => _mouseInput.IsMousePressed(button);
    public bool IsMouseReleased(MouseButton button) => _mouseInput.IsMouseReleased(button);

    public Vector2 MousePosition => _mouseInput.Position;
    public Vector2 MouseDelta => _mouseInput.PositionDelta;
    public Vector2 Scroll => _mouseInput.Scroll;

    public void Dispose()
    {
        _keyboardInput.Dispose();
        _mouseInput.Dispose();
    }
}