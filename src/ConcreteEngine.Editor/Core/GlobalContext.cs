namespace ConcreteEngine.Editor.Core;

internal sealed class GlobalContext(StateManager editorState)
{
    public readonly StateManager EditorState = editorState;
}