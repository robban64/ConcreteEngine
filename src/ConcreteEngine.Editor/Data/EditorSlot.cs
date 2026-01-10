namespace ConcreteEngine.Editor.Data;

public ref struct EditorSlot<T>(ref T state, ref long generation) where T : unmanaged
{
    public readonly ref T State = ref state;
    public readonly ref long Gen = ref generation;
}