using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Platform;
using Silk.NET.Input;

namespace ConcreteEngine.Engine.Editor;

internal sealed class EditorInputSourceImpl(EngineInputSource source) : EditorInputSource
{
    private EngineInputSource _source = source;

    public override bool IsKeyDown(Key key) => _source.HasKey(key, out var state) && state.IsHeld;

    public override bool IsKeyPressed(Key key) => _source.HasKey(key, out var state) && state.Pressed;
    
    public override bool IsMouseDown(MouseButton button) => _source.MouseButtons()[(int)button].IsHeld;

    public override bool IsMousePressed(MouseButton button) => _source.MouseButtons()[(int)button].Pressed;

}