namespace ConcreteEngine.Editor.Data;

public ref struct EditorSlot<T>(ref T state, ref long generation) where T : unmanaged
{
    public ref T State = ref state;
    public ref long Gen = ref generation;
}