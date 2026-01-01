using ConcreteEngine.Engine.Metadata.Input;
using Silk.NET.Input;

namespace ConcreteEngine.Editor;

public abstract class EditorInputSource
{
    public InputMouseState Mouse;
    
    public abstract bool IsKeyDown(Key key);
    public abstract bool IsKeyPressed(Key key);
    
    public abstract bool IsMouseDown(MouseButton button);

    public abstract bool IsMousePressed(MouseButton button);

}