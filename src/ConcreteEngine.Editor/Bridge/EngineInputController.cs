using ConcreteEngine.Core.Engine.Input;
using Silk.NET.Input;

namespace ConcreteEngine.Editor.Bridge;

public abstract class EngineInputController
{
    public InputMouseState Mouse;

    public bool HasEmptyKeyChars;
    public bool HasEmptyKeyInput;

    
    public abstract void ToggleBlockInput(bool block);

    public abstract ReadOnlySpan<Key> GetActiveKeys();
    public abstract ReadOnlySpan<char> GetKeyChars();

    public abstract bool IsKeyDown(Key key);
    public abstract bool IsKeyPressed(Key key);
    public abstract bool IsKeyUp(Key key);

    public abstract bool IsMouseDown(MouseButton button);
    public abstract bool IsMousePressed(MouseButton button);
}