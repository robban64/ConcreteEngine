using System.Numerics;
using Silk.NET.Input;

namespace ConcreteEngine.Core.Platform;

public interface IEngineInputSource
{
    public bool IsKeyDown(Key key);
    public bool IsKeyPressed(Key key);
    public bool IsKeyReleased(Key key) ;

    // Mouse API
    public bool IsMouseDown(MouseButton button);
    public bool IsMousePressed(MouseButton button) ;
    public bool IsMouseReleased(MouseButton button);

    public Vector2 MousePosition { get; }
    public Vector2 MouseDelta { get; }
    public Vector2 Scroll { get;  }


    void Update();
}
public sealed class EngineInputSource : IEngineInputSource
{
    private readonly KeyboardInput _keyboardInput;
    private readonly MouseInput _mouseInput;

    public EngineInputSource(IInputContext input)
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

    public Vector2 MousePosition => _mouseInput.Position;
    public Vector2 MouseDelta => _mouseInput.PositionDelta;
    public Vector2 Scroll => _mouseInput.Scroll;

    public void Dispose()
    {
        _keyboardInput.Dispose();
        _mouseInput.Dispose();
    }
}