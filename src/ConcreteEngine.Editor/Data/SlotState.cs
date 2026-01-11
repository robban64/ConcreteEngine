namespace ConcreteEngine.Editor.Data;

internal sealed class SlotState<T> where T : unmanaged
{
    public T State;
    public long Generation;
    public SlotView<T> GetView() => new(ref State, ref Generation);
}

public readonly ref struct SlotView<T>(ref T state, ref long generation) where T : unmanaged
{
    public readonly ref T State = ref state;
    public readonly ref long Gen = ref generation;
}